using Exceptionless;
using Microsoft.Extensions.DependencyInjection;
using PrintLog.DAL.Data;
using PrintLog.DAL.Models;
using PrintLog.Web.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TKSLibrary;

namespace PrintLog.Web.Jobs {
    public class Prisma {
        private readonly IServiceProvider serviceProvider;

        public Prisma(IServiceProvider serviceProvider) {
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
                            ImportFile newImport = new ImportFile() {
                                PrinterId = printerId,
                                FileName = Path.GetFileName(file),
                                CountLine = 0,
                                CountJob = 0,
                                IsSuccess = false,
                                DateCreated = dateTimeNow,
                            };
                            dbContext.ImportFiles.Add(newImport);
                            int rnt = dbContext.SaveChanges();

                            ImportFile importFile = new ImportFile();
                            if (rnt != -1) {
                                string filename = Path.GetFileName(file);
                                importFile = dbContext.ImportFiles.OrderByDescending(o => o.DateCreated).FirstOrDefault(w => w.FileName == filename);
                                newImport.ImportId = importFile.ImportId;
                            }

                            if (Path.GetExtension(file).ToLower() == ".acc") {
                                StreamReader reader = new StreamReader(file);
                                while (!reader.EndOfStream) {
                                    string line = reader.ReadLine();
                                    string[] value = line.Split(';');
                                    newImport.CountLine++;
                                    dbContext.SaveChanges();

                                    if (value[0] == "1010" && value[18] == string.Empty) {
                                        string jobId = value[1];
                                        MachineLog updateMachineLog = dbContext.MachineLogs.SingleOrDefault(s => s.PrinterId == printerId && s.JobId == jobId);
                                        if (updateMachineLog == null) {
                                            MachineLog newMachineLog = new MachineLog();
                                            newMachineLog.PrinterId = printerId;
                                            newMachineLog.JobId = jobId;
                                            newMachineLog.ImportId = newImport.ImportId;
                                            newMachineLog.TotalFile = value[4].ToInt();
                                            newMachineLog.Sender = value[5];
                                            newMachineLog.DateStart = DateTime.ParseExact(value[11] + " " + value[12], "dd.MM.yyyy HH:mm:ss", CultureInfoHelper.CultureInfoEN);
                                            newMachineLog.RawData = line;
                                            newMachineLog.DateCreated = DateTime.Now;
                                            dbContext.MachineLogs.Add(newMachineLog);
                                            newImport.CountJob++;
                                            dbContext.SaveChanges();
                                        }
                                    } else if (value[0] == "1011" && value[1].Length > 8) {
                                        string[] jobIdSplited = value[1].Split('.');
                                        string jobId = jobIdSplited[0];
                                        int fileId = (jobIdSplited[1]).ToInt();
                                        MachineLog updateMachineLog = dbContext.MachineLogs.SingleOrDefault(s => s.PrinterId == printerId && s.JobId == jobId);
                                        if (updateMachineLog == null) {
                                            MachineLog newMachineLog = new MachineLog();
                                            newMachineLog.PrinterId = printerId;
                                            newMachineLog.JobId = jobId;
                                            newMachineLog.ImportId = newImport.ImportId;
                                            newMachineLog.RawData = line;
                                            newMachineLog.DateCreated = DateTime.Now;
                                            dbContext.MachineLogs.Add(newMachineLog);
                                            newImport.CountJob++;
                                            dbContext.SaveChanges();
                                        }

                                        MachineLogDetail updateDetail = dbContext.MachineLogDetails.SingleOrDefault(s => s.PrinterId == printerId && s.JobId == jobId && s.FileId == fileId);
                                        if (updateDetail == null) {
                                            MachineLogDetail newDetail = new MachineLogDetail();
                                            newDetail.PrinterId = printerId;
                                            newDetail.JobId = jobId;
                                            newDetail.FileId = fileId;
                                            newDetail.ImportId = newImport.ImportId;
                                            newDetail.JobName = Path.GetFileName(value[3]);
                                            newDetail.JobType = value[2];
                                            newDetail.FullPath = value[3];
                                            newDetail.RawData = line;
                                            newDetail.DateCreated = DateTime.Now;
                                            dbContext.MachineLogDetails.Add(newDetail);
                                            dbContext.SaveChanges();
                                        } else {
                                            updateDetail.RawData += line;
                                            updateDetail.JobName = Path.GetFileName(value[3]);
                                            updateDetail.JobType = value[2];
                                            updateDetail.FullPath = value[3];
                                            updateDetail.ImportIdModified = newImport.ImportId;
                                            updateDetail.DateModified = DateTime.Now;
                                            dbContext.SaveChanges();
                                        }
                                    } else if (value[0] == "1040" && value[1].Length > 8) {
                                        string[] jobIdSplited = value[1].Split('.');
                                        string jobId = jobIdSplited[0];
                                        int fileId = (jobIdSplited[1]).ToInt();
                                        MachineLog updateMachineLog = dbContext.MachineLogs.SingleOrDefault(s => s.PrinterId == printerId && s.JobId == jobId);
                                        if (updateMachineLog == null) {
                                            MachineLog newMachineLog = new MachineLog();
                                            newMachineLog.PrinterId = printerId;
                                            newMachineLog.JobId = jobId;
                                            newMachineLog.ImportId = newImport.ImportId;
                                            newMachineLog.RawData = line;
                                            newMachineLog.DateCreated = DateTime.Now;
                                            dbContext.MachineLogs.Add(newMachineLog);
                                            newImport.CountJob++;
                                            dbContext.SaveChanges();
                                        }

                                        MachineLogDetail updateDetail = dbContext.MachineLogDetails.SingleOrDefault(s => s.PrinterId == printerId && s.JobId == jobId && s.FileId == fileId);
                                        if (updateDetail == null) {
                                            MachineLogDetail newDetail = new MachineLogDetail();
                                            newDetail.PrinterId = printerId;
                                            newDetail.JobId = jobId;
                                            newDetail.FileId = fileId;
                                            newDetail.ImportId = newImport.ImportId;
                                            newDetail.DateEnd = DateTime.ParseExact(value[4] + " " + value[5], "dd.MM.yyyy HH:mm:ss", CultureInfoHelper.CultureInfoEN);
                                            newDetail.TotalPage = value[18].ToInt();
                                            newDetail.RawData = line;
                                            newDetail.DateCreated = DateTime.Now;
                                            dbContext.MachineLogDetails.Add(newDetail);
                                            dbContext.SaveChanges();
                                        } else {
                                            updateDetail.RawData += line;
                                            updateDetail.DateEnd = DateTime.ParseExact(value[4] + " " + value[5], "dd.MM.yyyy HH:mm:ss", CultureInfoHelper.CultureInfoEN);
                                            updateDetail.TotalPage = value[18].ToInt();
                                            updateDetail.ImportIdModified = newImport.ImportId;
                                            updateDetail.DateModified = DateTime.Now;
                                            dbContext.SaveChanges();
                                        }
                                    } else if (value[0] == "1042" && value[1].Length > 8) {
                                        string[] jobIdSplited = value[1].Split('.');
                                        string jobId = jobIdSplited[0];
                                        int fileId = (jobIdSplited[1]).ToInt();
                                        MachineLog updateMachineLog = dbContext.MachineLogs.SingleOrDefault(s => s.PrinterId == printerId && s.JobId == jobId);
                                        if (updateMachineLog == null) {
                                            MachineLog newMachineLog = new MachineLog();
                                            newMachineLog.PrinterId = printerId;
                                            newMachineLog.JobId = jobId;
                                            newMachineLog.ImportId = newImport.ImportId;
                                            newMachineLog.RawData = line;
                                            newMachineLog.DateCreated = DateTime.Now;
                                            dbContext.MachineLogs.Add(newMachineLog);
                                            newImport.CountJob++;
                                            dbContext.SaveChanges();
                                        }

                                        MachineLogDetail updateDetail = dbContext.MachineLogDetails.SingleOrDefault(s => s.PrinterId == printerId && s.JobId == jobId && s.FileId == fileId);
                                        if (updateDetail == null) {
                                            MachineLogDetail newDetail = new MachineLogDetail();
                                            newDetail.PrinterId = printerId;
                                            newDetail.JobId = jobId;
                                            newDetail.FileId = fileId;
                                            newDetail.ImportId = newImport.ImportId;
                                            newDetail.TotalPage = value[3].ToInt();
                                            newDetail.RawData = line;
                                            newDetail.DateCreated = DateTime.Now;
                                            dbContext.MachineLogDetails.Add(newDetail);
                                            dbContext.SaveChanges();
                                        } else {
                                            updateDetail.RawData += line;
                                            updateDetail.TotalPage = value[3].ToInt();
                                            updateDetail.ImportIdModified = newImport.ImportId;
                                            updateDetail.DateModified = DateTime.Now;
                                            dbContext.SaveChanges();
                                        }
                                    } else {
                                        string[] jobIdSplited = value[1].Split('.');
                                        string jobId = jobIdSplited[0];
                                        MachineLog updateMachineLog = dbContext.MachineLogs.SingleOrDefault(s => s.PrinterId == printerId && s.JobId == jobId);
                                        if (updateMachineLog == null) {
                                            MachineLog newMachineLog = new MachineLog();
                                            newMachineLog.PrinterId = printerId;
                                            newMachineLog.JobId = jobId;
                                            newMachineLog.ImportId = newImport.ImportId;
                                            newMachineLog.RawData = line;
                                            newMachineLog.DateCreated = DateTime.Now;
                                            dbContext.MachineLogs.Add(newMachineLog);
                                            newImport.CountJob++;
                                            dbContext.SaveChanges();
                                        }
                                    }
                                }
                                newImport.IsSuccess = true;
                                dbContext.SaveChanges();
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
                        }
                    }
                }
            } catch(Exception ex) {
                ex.ToExceptionless().AddObject(printerId).Submit();
            }
        }
    }
}
