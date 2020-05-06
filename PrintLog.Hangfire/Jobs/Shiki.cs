using Exceptionless;
using Microsoft.Extensions.DependencyInjection;
using PrintLog.DAL.Data;
using PrintLog.DAL.Models;
using PrintLog.Hangfire.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TKSLibrary;

namespace PrintLog.Hangfire.Jobs {
    public class Shiki {
        private readonly IServiceProvider serviceProvider;

        public Shiki(IServiceProvider serviceProvider) {
            this.serviceProvider = serviceProvider;
        }

        public void ReadLog(int printerId) {
            try {
                using (var scope = serviceProvider.CreateScope()) {
                    var dbContext = scope.ServiceProvider.GetRequiredService<PrintlogDbContext>();
                    var printer = dbContext.MasterTypes.FirstOrDefault(f => f.TypeId == printerId);
                    if (printer != null) {
                        string printerName = printer.TypeName;

                        string pathInput = Path.Combine(Configs.RootPath, @"Inbox", printerName);
                        string pathOutputSuccess = Path.Combine(Configs.RootPath, @"Outbox\Successfully", printerName);
                        string pathOutputFailed = Path.Combine(Configs.RootPath, @"Outbox\Failed", printerName);

                        if (!Directory.Exists(pathOutputSuccess)) {
                            Directory.CreateDirectory(pathOutputSuccess);
                        }
                        if (!Directory.Exists(pathOutputFailed)) {
                            Directory.CreateDirectory(pathOutputFailed);
                        }

                        foreach (string file in Directory.EnumerateFiles(pathInput)) {
                            DateTime dateTimeNow = DateTime.Now;
                            List<MachineLog> newMachineLogs = new List<MachineLog>();
                            List<MachineLogDetail> newMachineLogDetails = new List<MachineLogDetail>();
                            ImportFile newImport = new ImportFile();
                            newImport.FileName = Path.GetFileName(file);
                            newImport.CountLine = 0;
                            newImport.CountJob = 0;
                            newImport.IsSuccess = false;
                            newImport.PrinterId = printerId;
                            newImport.DateCreated = dateTimeNow;
                            dbContext.ImportFiles.Add(newImport);
                            int rnt = dbContext.SaveChanges();

                            ImportFile importFile = new ImportFile();
                            if (rnt != -1) {
                                string filename = Path.GetFileName(file);
                                importFile = dbContext.ImportFiles.OrderByDescending(o => o.DateCreated).FirstOrDefault(w => w.FileName == filename);
                                newImport.ImportId = importFile.ImportId;
                            }

                            if (Path.GetExtension(file).ToLower() == ".csv") {
                                StreamReader reader = new StreamReader(file);
                                bool isHeader = true;
                                while (!reader.EndOfStream) {
                                    string line = reader.ReadLine();
                                    newImport.CountLine++;
                                    dbContext.SaveChanges();

                                    if (isHeader) {
                                        isHeader = false;
                                        continue;
                                    }
                                    string[] value = line.Split(',');
                                    string jobId = value[0];
                                    string status = value[14];
                                    if (status == "Normal") {
                                        MachineLog machineLog = dbContext.MachineLogs.SingleOrDefault(s => s.PrinterId == printerId && s.JobId == jobId);
                                        if (machineLog == null) {
                                            MachineLog newMachineLog = new MachineLog() { 
                                                PrinterId = printerId,
                                                JobId = jobId,
                                                ImportId = newImport.ImportId,
                                                TotalFile = 1,
                                                Sender = value[9],
                                                DateStart = DateTime.ParseExact(value[10], "yyyy/MM/dd HH:mm:ss", CultureInfoHelper.CultureInfoEN),
                                                RawData = line,
                                                DateCreated = dateTimeNow,
                                            };
                                            
                                            newMachineLogs.Add(newMachineLog);
                                            newImport.CountJob++;
                                        }

                                        MachineLogDetail machineLogDetail = dbContext.MachineLogDetails.SingleOrDefault(s => s.PrinterId == printerId && s.JobId == jobId && s.FileId == 1);
                                        if (machineLogDetail == null) {
                                            MachineLogDetail newDetail = new MachineLogDetail() {
                                                PrinterId = printerId,
                                                JobId = jobId,
                                                FileId = 1,
                                                ImportId = newImport.ImportId,
                                                JobName = Path.GetFileName(value[8]),
                                                JobType = value[4],
                                                FullPath = value[2],
                                                DateEnd = DateTime.ParseExact(value[12], "yyyy/MM/dd HH:mm:ss", CultureInfoHelper.CultureInfoEN),
                                                TotalPage = value[16].ToInt(),
                                                RawData = line,
                                                DateCreated = dateTimeNow,
                                            };
                                            
                                            newMachineLogDetails.Add(newDetail);
                                        }
                                    }
                                }
                                newImport.IsSuccess = true;
                                reader.Close();
                            }

                            if (newImport.IsSuccess) {
                                string pathSuccess = Path.Combine(pathOutputSuccess, Path.GetFileName(file));
                                int runningNo = 2;
                                while (File.Exists(pathSuccess)) {
                                    pathSuccess = Path.Combine(pathOutputSuccess, Path.GetFileNameWithoutExtension(file) + "_v" + runningNo + Path.GetExtension(file));
                                    runningNo++;
                                }
                                File.Copy(file, pathSuccess);
                                File.Delete(file);
                            } else {
                                string pathFailed = Path.Combine(pathOutputFailed, Path.GetFileName(file));
                                int runningNo = 2;
                                while (File.Exists(pathFailed)) {
                                    pathFailed = Path.Combine(pathOutputFailed, Path.GetFileNameWithoutExtension(file) + "_v" + runningNo + Path.GetExtension(file));
                                    runningNo++;
                                }
                                File.Copy(file, pathFailed);
                                File.Delete(file);
                            }

                            newImport.DateCreated = DateTime.Now;
                            if (newMachineLogs.Count > 0) {
                                dbContext.MachineLogs.AddRange(newMachineLogs);
                            }
                            dbContext.SaveChanges();
                        }
                    }
                }
            } catch (Exception ex) {
                ex.ToExceptionless().AddObject(printerId).Submit();
            }
        }
    }
}
