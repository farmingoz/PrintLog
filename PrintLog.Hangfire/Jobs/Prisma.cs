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

                        foreach (string file in Directory.EnumerateFiles(pathInput, "*.acc")) {
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

                            List<PrinterLog> LstPrinterLog = new List<PrinterLog>();
                            List<PrinterLogDetail> LstPrinterLogDetail = new List<PrinterLogDetail>();

                            StreamReader reader = new StreamReader(file);
                            while (!reader.EndOfStream) {
                                string line = reader.ReadLine();
                                newImport.CountLine++;

                                string[] value = line.Split(';');
                                string[] jobIdSplited = value[1].Split('.');
                                string jobId = jobIdSplited[0];
                                int recordType = value[0].ToInt();

                                if (recordType == 1010) {
                                    PrinterLog updatePrinterLog = LstPrinterLog.FirstOrDefault(f => f.JobId == jobId);
                                    if (updatePrinterLog == null) {
                                        LstPrinterLog.Add(new PrinterLog() {
                                            PrinterId = printerId,
                                            JobId = jobId,
                                            Client = value[2],
                                            Sender = value[3],
                                            TotalFile = value[4].ToInt(),
                                            Printer = value[5],
                                            Jobqueue = value[6].ToInt(),
                                            Resolution = value[7].ToInt(),
                                            Proofprint = value[8] == "yes",
                                            Storeprint = value[9] == "yes",
                                            CustAcc = value[10],
                                            DateSubmission = DateTime.ParseExact(value[11] + " " + value[12], "dd.MM.yyyy HH:mm:ss", CultureInfoHelper.CultureInfoEN),
                                            ReferenceId = value[13],
                                            Form = value[14],
                                            TicketName = value[15],
                                            JobName = value[16],
                                            OrderId = value[17],
                                            Range = value[18],
                                            ColorIds = value[19],
                                            TrackingEnabled = value[20] == "1",
                                            RawData = line,
                                            DateCreated = dateTimeNow,
                                            ImportId = newImport.ImportId,
                                            LastRecordType = recordType,
                                        });
                                        newImport.CountJob++;
                                    } else {
                                        updatePrinterLog.Client = value[2];
                                        updatePrinterLog.Sender = value[3];
                                        updatePrinterLog.TotalFile = value[4].ToInt();
                                        updatePrinterLog.Printer = value[5];
                                        updatePrinterLog.Jobqueue = value[6].ToInt();
                                        updatePrinterLog.Resolution = value[7].ToInt();
                                        updatePrinterLog.Proofprint = value[8] == "yes";
                                        updatePrinterLog.Storeprint = value[9] == "yes";
                                        updatePrinterLog.CustAcc = value[10];
                                        updatePrinterLog.DateSubmission = DateTime.ParseExact(value[11] + " " + value[12], "dd.MM.yyyy HH:mm:ss", CultureInfoHelper.CultureInfoEN);
                                        updatePrinterLog.ReferenceId = value[13];
                                        updatePrinterLog.Form = value[14];
                                        updatePrinterLog.TicketName = value[15];
                                        updatePrinterLog.JobName = value[16];
                                        updatePrinterLog.OrderId = value[17];
                                        updatePrinterLog.Range = value[18];
                                        updatePrinterLog.ColorIds = value[19];
                                        updatePrinterLog.TrackingEnabled = value[20] == "1";
                                        updatePrinterLog.RawData += line;
                                        updatePrinterLog.ImportIdModified = newImport.ImportId;
                                        updatePrinterLog.DateModified = dateTimeNow;
                                        updatePrinterLog.LastRecordType = recordType;
                                    }
                                } else if (recordType == 1011) {
                                    PrinterLog updatePrinterLog = LstPrinterLog.FirstOrDefault(f => f.JobId == jobId);
                                    if (updatePrinterLog == null) {
                                        LstPrinterLog.Add(new PrinterLog() {
                                            PrinterId = printerId,
                                            JobId = jobId,
                                            RawData = line,
                                            DateCreated = dateTimeNow,
                                            ImportId = newImport.ImportId,
                                            LastRecordType = recordType,
                                        });
                                        newImport.CountJob++;
                                    }

                                    PrinterLogDetail updatePrinterLogDetail = LstPrinterLogDetail.FirstOrDefault(f => f.JobId == jobId);
                                    if (updatePrinterLogDetail == null) {
                                        LstPrinterLogDetail.Add(new PrinterLogDetail() {
                                            PrinterId = printerId,
                                            JobId = jobId,
                                            JobType = value[2],
                                            FullPath = value[3],
                                            JobName = Path.GetFileName(value[3]),
                                            Filesize = value[4].ToInt(),
                                            Copies = value[5].ToInt(),
                                            Formdef = value[6],
                                            Pagedef = value[7],
                                            RawData = line,
                                            DateCreated = dateTimeNow,
                                            ImportId = newImport.ImportId,
                                            LastRecordType = recordType,
                                        });
                                        newImport.CountJob++;
                                    } else {
                                        updatePrinterLogDetail.JobType = value[2];
                                        updatePrinterLogDetail.FullPath = value[3];
                                        updatePrinterLogDetail.JobName = Path.GetFileName(value[3]);
                                        updatePrinterLogDetail.Filesize = value[4].ToInt();
                                        updatePrinterLogDetail.Copies = value[5].ToInt();
                                        updatePrinterLogDetail.Formdef = value[6];
                                        updatePrinterLogDetail.Pagedef = value[7];
                                        updatePrinterLogDetail.RawData += line;
                                        updatePrinterLogDetail.ImportIdModified = newImport.ImportId;
                                        updatePrinterLogDetail.DateModified = dateTimeNow;
                                        updatePrinterLogDetail.LastRecordType = recordType;
                                    }
                                }
                            }





                            dbContext.SaveChanges();
                            
                            ///

                            if (Path.GetExtension(file).ToLower() == ".acc") {
                                StreamReader reader = new StreamReader(file);
                                while (!reader.EndOfStream) {
                                    string line = reader.ReadLine();
                                    string[] value = line.Split(';');
                                    newImport.CountLine++;
                                    dbContext.SaveChanges();

                                    // TODO - Convert data into DB

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
