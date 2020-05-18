using Exceptionless;
using Exceptionless.Extensions;
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
                        int cycleTime = printer.CycleTime.Value;
                        string maxJobId = dbContext.PrinterLogs.Where(w => w.PrinterId == printerId && w.CycleTime == cycleTime).Max(m => m.JobId);

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
                                CountJobDetail = 0,
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

                                #region recordType == 1000
                                if (recordType == 1000) {
                                    PrinterLog updatePrinterLog = LstPrinterLog.FirstOrDefault(f => f.JobId == jobId && f.CycleTime == cycleTime);
                                    if (updatePrinterLog == null) {
                                        LstPrinterLog.Add(new PrinterLog() {
                                            PrinterId = printerId,
                                            JobId = jobId,
                                            CycleTime = cycleTime,
                                            RawData = line,
                                            DateCreated = dateTimeNow,
                                            ImportId = newImport.ImportId,
                                            LastRecordType = recordType,
                                        });
                                        newImport.CountJob++;
                                    }
                                }
                                #endregion
                                #region recordType == 1010
                                else if (recordType == 1010) {
                                    PrinterLog updatePrinterLog = LstPrinterLog.FirstOrDefault(f => f.JobId == jobId && f.CycleTime == cycleTime);
                                    if (updatePrinterLog == null) {
                                        LstPrinterLog.Add(new PrinterLog() {
                                            PrinterId = printerId,
                                            JobId = jobId,
                                            CycleTime = cycleTime,
                                            RawData = line,
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
                                            DateCreated = dateTimeNow,
                                            ImportId = newImport.ImportId,
                                            LastRecordType = recordType,
                                        });
                                        newImport.CountJob++;
                                    } else {
                                        updatePrinterLog.RawData += line;
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
                                        updatePrinterLog.ImportIdModified = newImport.ImportId;
                                        updatePrinterLog.DateModified = dateTimeNow;
                                        updatePrinterLog.LastRecordType = recordType;
                                    }
                                }
                                #endregion
                                #region recordType == 1011
                                else if (recordType == 1011) {
                                    PrinterLog updatePrinterLog = LstPrinterLog.FirstOrDefault(f => f.JobId == jobId && f.CycleTime == cycleTime);
                                    if (updatePrinterLog == null) {
                                        LstPrinterLog.Add(new PrinterLog() {
                                            PrinterId = printerId,
                                            JobId = jobId,
                                            CycleTime = cycleTime,
                                            RawData = line,
                                            DateCreated = dateTimeNow,
                                            ImportId = newImport.ImportId,
                                            LastRecordType = recordType,
                                        });
                                        newImport.CountJob++;
                                    }

                                    if (jobIdSplited.Length > 1) {
                                        int fileId = jobIdSplited[1].ToInt();
                                        PrinterLogDetail updatePrinterLogDetail = LstPrinterLogDetail.FirstOrDefault(f => f.JobId == jobId && f.FileId == fileId && f.CycleTime == cycleTime);
                                        if (updatePrinterLogDetail == null) {
                                            LstPrinterLogDetail.Add(new PrinterLogDetail() {
                                                PrinterId = printerId,
                                                JobId = jobId,
                                                FileId = fileId,
                                                CycleTime = cycleTime,
                                                RawData = line,
                                                JobType = value[2],
                                                FullPath = value[3],
                                                JobName = Path.GetFileName(value[3]),
                                                Filesize = value[4].ToInt(),
                                                Copies = value[5].ToInt(),
                                                Formdef = value[6],
                                                Pagedef = value[7],
                                                DateCreated = dateTimeNow,
                                                ImportId = newImport.ImportId,
                                                LastRecordType = recordType,
                                            });
                                            newImport.CountJobDetail++;
                                        } else {
                                            updatePrinterLogDetail.RawData += line;
                                            updatePrinterLogDetail.JobType = value[2];
                                            updatePrinterLogDetail.FullPath = value[3];
                                            updatePrinterLogDetail.JobName = Path.GetFileName(value[3]);
                                            updatePrinterLogDetail.Filesize = value[4].ToInt();
                                            updatePrinterLogDetail.Copies = value[5].ToInt();
                                            updatePrinterLogDetail.Formdef = value[6];
                                            updatePrinterLogDetail.Pagedef = value[7];
                                            updatePrinterLogDetail.ImportIdModified = newImport.ImportId;
                                            updatePrinterLogDetail.DateModified = dateTimeNow;
                                            updatePrinterLogDetail.LastRecordType = recordType;
                                        }
                                    }
                                }
                                #endregion
                                #region recordType == 1012
                                else if (recordType == 1012) {
                                    PrinterLog updatePrinterLog = LstPrinterLog.FirstOrDefault(f => f.JobId == jobId && f.CycleTime == cycleTime);
                                    if (updatePrinterLog == null) {
                                        LstPrinterLog.Add(new PrinterLog() {
                                            PrinterId = printerId,
                                            JobId = jobId,
                                            CycleTime = cycleTime,
                                            RawData = line,
                                            UserExplorer = value[2],
                                            StatusExplorer = value[3].ToInt(),
                                            DateExplorer = DateTime.ParseExact(value[4] + " " + value[5], "dd.MM.yyyy HH:mm:ss", CultureInfoHelper.CultureInfoEN),
                                            DateCreated = dateTimeNow,
                                            ImportId = newImport.ImportId,
                                            LastRecordType = recordType,
                                        });
                                        newImport.CountJob++;
                                    } else {
                                        updatePrinterLog.RawData += line;
                                        updatePrinterLog.UserExplorer = value[2];
                                        updatePrinterLog.StatusExplorer = value[3].ToInt();
                                        updatePrinterLog.DateExplorer = DateTime.ParseExact(value[4] + " " + value[5], "dd.MM.yyyy HH:mm:ss", CultureInfoHelper.CultureInfoEN);
                                        updatePrinterLog.LastRecordType = recordType;
                                        updatePrinterLog.DateModified = dateTimeNow;
                                    }
                                }
                                #endregion

                                #region recordType == 1020
                                else if (recordType == 1020) {
                                    PrinterLog updatePrinterLog = LstPrinterLog.FirstOrDefault(f => f.JobId == jobId && f.CycleTime == cycleTime);
                                    if (updatePrinterLog == null) {
                                        LstPrinterLog.Add(new PrinterLog() {
                                            PrinterId = printerId,
                                            JobId = jobId,
                                            CycleTime = cycleTime,
                                            RawData = line,
                                            DateCreated = dateTimeNow,
                                            ImportId = newImport.ImportId,
                                            LastRecordType = recordType,
                                        });
                                        newImport.CountJob++;
                                    }
                                }
                                #endregion

                                #region recordType == 1030
                                else if (recordType == 1030) {
                                    if (jobIdSplited.Length == 1) {
                                        PrinterLog updatePrinterLog = LstPrinterLog.FirstOrDefault(f => f.JobId == jobId && f.CycleTime == cycleTime);
                                        if (updatePrinterLog == null) {
                                            LstPrinterLog.Add(new PrinterLog() {
                                                PrinterId = printerId,
                                                JobId = jobId,
                                                CycleTime = cycleTime,
                                                RawData = line,
                                                Sender = value[2],
                                                JobType = value[3],
                                                JobName = value[4],
                                                Printer = value[5],
                                                Jobqueue = value[6].ToInt(),
                                                Resolution = value[7].ToInt(),
                                                DateSubmission = DateTime.ParseExact(value[8] + " " + value[9], "dd.MM.yyyy HH:mm:ss", CultureInfoHelper.CultureInfoEN),
                                                DateCreated = dateTimeNow,
                                                ImportId = newImport.ImportId,
                                                LastRecordType = recordType,
                                            });
                                            newImport.CountJob++;
                                        } else {
                                            updatePrinterLog.RawData += line;
                                            updatePrinterLog.Sender = value[2];
                                            updatePrinterLog.JobType = value[3];
                                            updatePrinterLog.JobName = value[4];
                                            updatePrinterLog.Printer = value[5];
                                            updatePrinterLog.Jobqueue = value[6].ToInt();
                                            updatePrinterLog.Resolution = value[7].ToInt();
                                            updatePrinterLog.DateSubmission = DateTime.ParseExact(value[8] + " " + value[9], "dd.MM.yyyy HH:mm:ss", CultureInfoHelper.CultureInfoEN);
                                            updatePrinterLog.LastRecordType = recordType;
                                            updatePrinterLog.DateModified = dateTimeNow;
                                        }
                                    } else {
                                        int fileId = jobIdSplited[1].ToInt();
                                        PrinterLog updatePrinterLog = LstPrinterLog.FirstOrDefault(f => f.JobId == jobId && f.CycleTime == cycleTime);
                                        if (updatePrinterLog == null) {
                                            LstPrinterLog.Add(new PrinterLog() {
                                                PrinterId = printerId,
                                                JobId = jobId,
                                                CycleTime = cycleTime,
                                                RawData = line,
                                                Sender = value[2],
                                                JobType = value[3],
                                                Printer = value[5],
                                                Jobqueue = value[6].ToInt(),
                                                Resolution = value[7].ToInt(),
                                                DateSubmission = DateTime.ParseExact(value[8] + " " + value[9], "dd.MM.yyyy HH:mm:ss", CultureInfoHelper.CultureInfoEN),
                                                DateCreated = dateTimeNow,
                                                ImportId = newImport.ImportId,
                                                LastRecordType = recordType,
                                            });
                                            newImport.CountJob++;
                                        } else {
                                            updatePrinterLog.RawData += line;
                                            updatePrinterLog.Sender = value[2];
                                            updatePrinterLog.JobType = value[3];
                                            updatePrinterLog.Printer = value[5];
                                            updatePrinterLog.Jobqueue = value[6].ToInt();
                                            updatePrinterLog.Resolution = value[7].ToInt();
                                            updatePrinterLog.DateSubmission = DateTime.ParseExact(value[8] + " " + value[9], "dd.MM.yyyy HH:mm:ss", CultureInfoHelper.CultureInfoEN);
                                            updatePrinterLog.LastRecordType = recordType;
                                            updatePrinterLog.DateModified = dateTimeNow;
                                        }

                                        PrinterLogDetail updatePrinterLogDetail = LstPrinterLogDetail.FirstOrDefault(f => f.JobId == jobId && f.FileId == fileId && f.CycleTime == cycleTime);
                                        if (updatePrinterLogDetail == null) {
                                            LstPrinterLogDetail.Add(new PrinterLogDetail() {
                                                PrinterId = printerId,
                                                JobId = jobId,
                                                FileId = fileId,
                                                CycleTime = cycleTime,
                                                RawData = line,
                                                JobName = value[4],
                                                Printer = value[5],
                                                Jobqueue = value[6].ToInt(),
                                                DateSubmission = DateTime.ParseExact(value[8] + " " + value[9], "dd.MM.yyyy HH:mm:ss", CultureInfoHelper.CultureInfoEN),
                                                Copies = value[10].ToInt(),
                                                DateCreated = dateTimeNow,
                                                ImportId = newImport.ImportId,
                                                LastRecordType = recordType,
                                            });
                                            newImport.CountJobDetail++;
                                        } else {
                                            updatePrinterLogDetail.RawData += line;
                                            updatePrinterLogDetail.JobName = value[4];
                                            updatePrinterLogDetail.Printer = value[5];
                                            updatePrinterLogDetail.Jobqueue = value[6].ToInt();
                                            updatePrinterLogDetail.DateSubmission = DateTime.ParseExact(value[8] + " " + value[9], "dd.MM.yyyy HH:mm:ss", CultureInfoHelper.CultureInfoEN);
                                            updatePrinterLogDetail.Copies = value[10].ToInt();
                                            updatePrinterLogDetail.ImportIdModified = newImport.ImportId;
                                            updatePrinterLogDetail.DateModified = dateTimeNow;
                                            updatePrinterLogDetail.LastRecordType = recordType;
                                        }
                                    }
                                }
                                #endregion
                                #region recordType == 1031
                                else if (recordType == 1031) {
                                    if (jobIdSplited.Length == 1) {
                                        PrinterLog updatePrinterLog = LstPrinterLog.FirstOrDefault(f => f.JobId == jobId && f.CycleTime == cycleTime);
                                        if (updatePrinterLog == null) {
                                            LstPrinterLog.Add(new PrinterLog() {
                                                PrinterId = printerId,
                                                JobId = jobId,
                                                CycleTime = cycleTime,
                                                RawData = line,
                                                UserExplorer = value[2],
                                                StatusExplorer = value[3].ToInt(),
                                                Printer = value[4],
                                                Jobqueue = value[5].ToInt(),
                                                Range = value[6],
                                                Form = value[7],
                                                JobName = value[8],
                                                DateStartQueue = DateTime.ParseExact(value[9] + " " + value[10], "dd.MM.yyyy HH:mm:ss", CultureInfoHelper.CultureInfoEN),
                                                DateEndQueue = DateTime.ParseExact(value[11] + " " + value[12], "dd.MM.yyyy HH:mm:ss", CultureInfoHelper.CultureInfoEN),
                                                DateCreated = dateTimeNow,
                                                ImportId = newImport.ImportId,
                                                LastRecordType = recordType,
                                            });
                                            newImport.CountJob++;
                                        } else {
                                            updatePrinterLog.RawData += line;
                                            updatePrinterLog.UserExplorer = value[2];
                                            updatePrinterLog.StatusExplorer = value[3].ToInt();
                                            updatePrinterLog.Printer = value[4];
                                            updatePrinterLog.Jobqueue = value[5].ToInt();
                                            updatePrinterLog.Range = value[6];
                                            updatePrinterLog.Form = value[7];
                                            updatePrinterLog.JobName = value[8];
                                            updatePrinterLog.DateStartQueue = DateTime.ParseExact(value[9] + " " + value[10], "dd.MM.yyyy HH:mm:ss", CultureInfoHelper.CultureInfoEN);
                                            updatePrinterLog.DateEndQueue = DateTime.ParseExact(value[11] + " " + value[12], "dd.MM.yyyy HH:mm:ss", CultureInfoHelper.CultureInfoEN);
                                            updatePrinterLog.LastRecordType = recordType;
                                            updatePrinterLog.DateModified = dateTimeNow;
                                        }
                                    } else {
                                        int fileId = jobIdSplited[1].ToInt();
                                        PrinterLog updatePrinterLog = LstPrinterLog.FirstOrDefault(f => f.JobId == jobId && f.CycleTime == cycleTime);
                                        if (updatePrinterLog == null) {
                                            LstPrinterLog.Add(new PrinterLog() {
                                                PrinterId = printerId,
                                                JobId = jobId,
                                                CycleTime = cycleTime,
                                                RawData = line,
                                                UserExplorer = value[2],
                                                StatusExplorer = value[3].ToInt(),
                                                Printer = value[4],
                                                Jobqueue = value[5].ToInt(),
                                                Range = value[6],
                                                Form = value[7],
                                                DateStartQueue = DateTime.ParseExact(value[9] + " " + value[10], "dd.MM.yyyy HH:mm:ss", CultureInfoHelper.CultureInfoEN),
                                                DateEndQueue = DateTime.ParseExact(value[11] + " " + value[12], "dd.MM.yyyy HH:mm:ss", CultureInfoHelper.CultureInfoEN),
                                                DateCreated = dateTimeNow,
                                                ImportId = newImport.ImportId,
                                                LastRecordType = recordType,
                                            });
                                            newImport.CountJob++;
                                        } else {
                                            updatePrinterLog.RawData += line;
                                            updatePrinterLog.UserExplorer = value[2];
                                            updatePrinterLog.StatusExplorer = value[3].ToInt();
                                            updatePrinterLog.Printer = value[4];
                                            updatePrinterLog.Jobqueue = value[5].ToInt();
                                            updatePrinterLog.Range = value[6];
                                            updatePrinterLog.Form = value[7];
                                            updatePrinterLog.DateStartQueue = DateTime.ParseExact(value[9] + " " + value[10], "dd.MM.yyyy HH:mm:ss", CultureInfoHelper.CultureInfoEN);
                                            updatePrinterLog.DateEndQueue = DateTime.ParseExact(value[11] + " " + value[12], "dd.MM.yyyy HH:mm:ss", CultureInfoHelper.CultureInfoEN);
                                            updatePrinterLog.LastRecordType = recordType;
                                            updatePrinterLog.DateModified = dateTimeNow;
                                        }

                                        PrinterLogDetail updatePrinterLogDetail = LstPrinterLogDetail.FirstOrDefault(f => f.JobId == jobId && f.FileId == fileId && f.CycleTime == cycleTime);
                                        if (updatePrinterLogDetail == null) {
                                            LstPrinterLogDetail.Add(new PrinterLogDetail() {
                                                PrinterId = printerId,
                                                JobId = jobId,
                                                FileId = fileId,
                                                CycleTime = cycleTime,
                                                RawData = line,
                                                UserExplorer = value[2],
                                                StatusExplorer = value[3].ToInt(),
                                                Printer = value[4],
                                                Jobqueue = value[5].ToInt(),
                                                Range = value[6],
                                                Form = value[7],
                                                JobName = value[8],
                                                DateStartQueue = DateTime.ParseExact(value[9] + " " + value[10], "dd.MM.yyyy HH:mm:ss", CultureInfoHelper.CultureInfoEN),
                                                DateEndQueue = DateTime.ParseExact(value[11] + " " + value[12], "dd.MM.yyyy HH:mm:ss", CultureInfoHelper.CultureInfoEN),
                                                Copies = value[13].ToInt(),
                                                DateCreated = dateTimeNow,
                                                ImportId = newImport.ImportId,
                                                LastRecordType = recordType,
                                            });
                                            newImport.CountJobDetail++;
                                        } else {
                                            updatePrinterLogDetail.RawData += line;
                                            updatePrinterLogDetail.UserExplorer = value[2];
                                            updatePrinterLogDetail.StatusExplorer = value[3].ToInt();
                                            updatePrinterLogDetail.Printer = value[4];
                                            updatePrinterLogDetail.Jobqueue = value[5].ToInt();
                                            updatePrinterLogDetail.Range = value[6];
                                            updatePrinterLogDetail.Form = value[7];
                                            updatePrinterLogDetail.JobName = value[8];
                                            updatePrinterLogDetail.DateStartQueue = DateTime.ParseExact(value[9] + " " + value[10], "dd.MM.yyyy HH:mm:ss", CultureInfoHelper.CultureInfoEN);
                                            updatePrinterLogDetail.DateEndQueue = DateTime.ParseExact(value[11] + " " + value[12], "dd.MM.yyyy HH:mm:ss", CultureInfoHelper.CultureInfoEN);
                                            updatePrinterLogDetail.Copies = value[13].ToInt();
                                            updatePrinterLogDetail.ImportIdModified = newImport.ImportId;
                                            updatePrinterLogDetail.DateModified = dateTimeNow;
                                            updatePrinterLogDetail.LastRecordType = recordType;
                                        }
                                    }
                                }
                                #endregion
                                #region recordType == 1032
                                else if (recordType == 1032) {
                                    PrinterLog updatePrinterLog = LstPrinterLog.FirstOrDefault(f => f.JobId == jobId && f.CycleTime == cycleTime);
                                    if (updatePrinterLog == null) {
                                        LstPrinterLog.Add(new PrinterLog() {
                                            PrinterId = printerId,
                                            JobId = jobId,
                                            CycleTime = cycleTime,
                                            RawData = line,
                                            DateCreated = dateTimeNow,
                                            ImportId = newImport.ImportId,
                                            LastRecordType = recordType,
                                        });
                                        newImport.CountJob++;
                                    }
                                }
                                #endregion
                                #region recordType == 1033
                                else if (recordType == 1033) {
                                    PrinterLog updatePrinterLog = LstPrinterLog.FirstOrDefault(f => f.JobId == jobId && f.CycleTime == cycleTime);
                                    if (updatePrinterLog == null) {
                                        LstPrinterLog.Add(new PrinterLog() {
                                            PrinterId = printerId,
                                            JobId = jobId,
                                            CycleTime = cycleTime,
                                            RawData = line,
                                            DateCreated = dateTimeNow,
                                            ImportId = newImport.ImportId,
                                            LastRecordType = recordType,
                                        });
                                        newImport.CountJob++;
                                    }
                                }
                                #endregion

                                #region recordType == 1040
                                else if (recordType == 1040) {
                                    if (jobIdSplited.Length == 1) {
                                        PrinterLog updatePrinterLog = LstPrinterLog.FirstOrDefault(f => f.JobId == jobId && f.CycleTime == cycleTime);
                                        if (updatePrinterLog == null) {
                                            LstPrinterLog.Add(new PrinterLog() {
                                                PrinterId = printerId,
                                                JobId = jobId,
                                                CycleTime = cycleTime,
                                                RawData = line,
                                                DateStart = DateTime.ParseExact(value[2] + " " + value[3], "dd.MM.yyyy HH:mm:ss", CultureInfoHelper.CultureInfoEN),
                                                DateEnd = DateTime.ParseExact(value[4] + " " + value[5], "dd.MM.yyyy HH:mm:ss", CultureInfoHelper.CultureInfoEN),
                                                Simplex = value[6].ToBoolean(),
                                                Duplex = value[7].ToBoolean(),
                                                TotalPage = value[10].ToInt(),
                                                OriginalPages = value[11].ToInt(),
                                                FrontPages = value[12].ToInt(),
                                                BackPages = value[13].ToInt(),
                                                TotalSheets = value[14].ToInt(),
                                                InformationPages = value[15].ToInt(),
                                                InformationSheets = value[16].ToInt(),
                                                TotalOffsets = value[17].ToInt(),
                                                Feet = value[18].ToInt(),
                                                Nup = value[20].ToInt(),
                                                DateCreated = dateTimeNow,
                                                ImportId = newImport.ImportId,
                                                LastRecordType = recordType,
                                            });
                                            newImport.CountJob++;
                                        } else {
                                            updatePrinterLog.RawData += line;
                                            updatePrinterLog.DateStart = DateTime.ParseExact(value[2] + " " + value[3], "dd.MM.yyyy HH:mm:ss", CultureInfoHelper.CultureInfoEN);
                                            updatePrinterLog.DateEnd = DateTime.ParseExact(value[4] + " " + value[5], "dd.MM.yyyy HH:mm:ss", CultureInfoHelper.CultureInfoEN);
                                            updatePrinterLog.Simplex = value[6].ToBoolean();
                                            updatePrinterLog.Duplex = value[7].ToBoolean();
                                            updatePrinterLog.TotalPage = value[10].ToInt();
                                            updatePrinterLog.OriginalPages = value[11].ToInt();
                                            updatePrinterLog.FrontPages = value[12].ToInt();
                                            updatePrinterLog.BackPages = value[13].ToInt();
                                            updatePrinterLog.TotalSheets = value[14].ToInt();
                                            updatePrinterLog.InformationPages = value[15].ToInt();
                                            updatePrinterLog.InformationSheets = value[16].ToInt();
                                            updatePrinterLog.TotalOffsets = value[17].ToInt();
                                            updatePrinterLog.Feet = value[18].ToInt();
                                            updatePrinterLog.Nup = value[20].ToInt();
                                            updatePrinterLog.LastRecordType = recordType;
                                            updatePrinterLog.DateModified = dateTimeNow;
                                        }
                                    } else {
                                        int fileId = jobIdSplited[1].ToInt();
                                        PrinterLog updatePrinterLog = LstPrinterLog.FirstOrDefault(f => f.JobId == jobId && f.CycleTime == cycleTime);
                                        if (updatePrinterLog == null) {
                                            LstPrinterLog.Add(new PrinterLog() {
                                                PrinterId = printerId,
                                                JobId = jobId,
                                                CycleTime = cycleTime,
                                                RawData = line,
                                                DateStart = DateTime.ParseExact(value[2] + " " + value[3], "dd.MM.yyyy HH:mm:ss", CultureInfoHelper.CultureInfoEN),
                                                DateEnd = DateTime.ParseExact(value[4] + " " + value[5], "dd.MM.yyyy HH:mm:ss", CultureInfoHelper.CultureInfoEN),
                                                Simplex = value[6].ToBoolean(),
                                                Duplex = value[7].ToBoolean(),
                                                TotalPage = value[10].ToInt(),
                                                OriginalPages = value[11].ToInt(),
                                                FrontPages = value[12].ToInt(),
                                                BackPages = value[13].ToInt(),
                                                TotalSheets = value[14].ToInt(),
                                                InformationPages = value[15].ToInt(),
                                                InformationSheets = value[16].ToInt(),
                                                TotalOffsets = value[17].ToInt(),
                                                Feet = value[18].ToInt(),
                                                Nup = value[20].ToInt(),
                                                DateCreated = dateTimeNow,
                                                ImportId = newImport.ImportId,
                                                LastRecordType = recordType,
                                            });
                                            newImport.CountJob++;
                                        } else {
                                            updatePrinterLog.RawData += line;
                                            updatePrinterLog.DateStart = DateTime.ParseExact(value[2] + " " + value[3], "dd.MM.yyyy HH:mm:ss", CultureInfoHelper.CultureInfoEN);
                                            updatePrinterLog.DateEnd = DateTime.ParseExact(value[4] + " " + value[5], "dd.MM.yyyy HH:mm:ss", CultureInfoHelper.CultureInfoEN);
                                            updatePrinterLog.Simplex = value[6].ToBoolean();
                                            updatePrinterLog.Duplex = value[7].ToBoolean();
                                            updatePrinterLog.TotalPage = value[10].ToInt();
                                            updatePrinterLog.OriginalPages = value[11].ToInt();
                                            updatePrinterLog.FrontPages = value[12].ToInt();
                                            updatePrinterLog.BackPages = value[13].ToInt();
                                            updatePrinterLog.TotalSheets = value[14].ToInt();
                                            updatePrinterLog.InformationPages = value[15].ToInt();
                                            updatePrinterLog.InformationSheets = value[16].ToInt();
                                            updatePrinterLog.TotalOffsets = value[17].ToInt();
                                            updatePrinterLog.Feet = value[18].ToInt();
                                            updatePrinterLog.Nup = value[20].ToInt();
                                            updatePrinterLog.LastRecordType = recordType;
                                            updatePrinterLog.DateModified = dateTimeNow;
                                        }

                                        PrinterLogDetail updatePrinterLogDetail = LstPrinterLogDetail.FirstOrDefault(f => f.JobId == jobId && f.FileId == fileId && f.CycleTime == cycleTime);
                                        if (updatePrinterLogDetail == null) {
                                            LstPrinterLogDetail.Add(new PrinterLogDetail() {
                                                PrinterId = printerId,
                                                JobId = jobId,
                                                FileId = fileId,
                                                CycleTime = cycleTime,
                                                RawData = line,
                                                DateStart = DateTime.ParseExact(value[2] + " " + value[3], "dd.MM.yyyy HH:mm:ss", CultureInfoHelper.CultureInfoEN),
                                                DateEnd = DateTime.ParseExact(value[4] + " " + value[5], "dd.MM.yyyy HH:mm:ss", CultureInfoHelper.CultureInfoEN),
                                                Simplex = value[6].ToBoolean(),
                                                Duplex = value[7].ToBoolean(),
                                                TotalPage = value[10].ToInt(),
                                                OriginalPages = value[11].ToInt(),
                                                FrontPages = value[12].ToInt(),
                                                BackPages = value[13].ToInt(),
                                                TotalSheets = value[14].ToInt(),
                                                InformationPages = value[15].ToInt(),
                                                InformationSheets = value[16].ToInt(),
                                                TotalOffsets = value[17].ToInt(),
                                                Feet = value[18].ToInt(),
                                                Nup = value[20].ToInt(),
                                                DateCreated = dateTimeNow,
                                                ImportId = newImport.ImportId,
                                                LastRecordType = recordType,
                                            });
                                            newImport.CountJobDetail++;
                                        } else {
                                            updatePrinterLogDetail.RawData += line;
                                            updatePrinterLogDetail.DateStart = DateTime.ParseExact(value[2] + " " + value[3], "dd.MM.yyyy HH:mm:ss", CultureInfoHelper.CultureInfoEN);
                                            updatePrinterLogDetail.DateEnd = DateTime.ParseExact(value[4] + " " + value[5], "dd.MM.yyyy HH:mm:ss", CultureInfoHelper.CultureInfoEN);
                                            updatePrinterLogDetail.Simplex = value[6].ToBoolean();
                                            updatePrinterLogDetail.Duplex = value[7].ToBoolean();
                                            updatePrinterLogDetail.TotalPage = value[10].ToInt();
                                            updatePrinterLogDetail.OriginalPages = value[11].ToInt();
                                            updatePrinterLogDetail.FrontPages = value[12].ToInt();
                                            updatePrinterLogDetail.BackPages = value[13].ToInt();
                                            updatePrinterLogDetail.TotalSheets = value[14].ToInt();
                                            updatePrinterLogDetail.InformationPages = value[15].ToInt();
                                            updatePrinterLogDetail.InformationSheets = value[16].ToInt();
                                            updatePrinterLogDetail.TotalOffsets = value[17].ToInt();
                                            updatePrinterLogDetail.Feet = value[18].ToInt();
                                            updatePrinterLogDetail.Nup = value[20].ToInt();
                                            updatePrinterLogDetail.ImportIdModified = newImport.ImportId;
                                            updatePrinterLogDetail.DateModified = dateTimeNow;
                                            updatePrinterLogDetail.LastRecordType = recordType;
                                        }
                                    }
                                }
                                #endregion
                                #region recordType == 1041
                                else if (recordType == 1041) {
                                    if (jobIdSplited.Length == 1) {
                                        PrinterLog updatePrinterLog = LstPrinterLog.FirstOrDefault(f => f.JobId == jobId && f.CycleTime == cycleTime);
                                        if (updatePrinterLog == null) {
                                            LstPrinterLog.Add(new PrinterLog() {
                                                PrinterId = printerId,
                                                JobId = jobId,
                                                CycleTime = cycleTime,
                                                RawData = line,
                                                DateCreated = dateTimeNow,
                                                ImportId = newImport.ImportId,
                                                LastRecordType = recordType,
                                            });
                                            newImport.CountJob++;
                                        }
                                    } else {
                                        int fileId = jobIdSplited[1].ToInt();
                                        PrinterLogDetail updatePrinterLogDetail = LstPrinterLogDetail.FirstOrDefault(f => f.JobId == jobId && f.FileId == fileId && f.CycleTime == cycleTime);
                                        if (updatePrinterLogDetail == null) {
                                            LstPrinterLogDetail.Add(new PrinterLogDetail() {
                                                PrinterId = printerId,
                                                JobId = jobId,
                                                FileId = fileId,
                                                CycleTime = cycleTime,
                                                RawData = line,
                                                DateCreated = dateTimeNow,
                                                ImportId = newImport.ImportId,
                                                LastRecordType = recordType,
                                            });
                                            newImport.CountJobDetail++;
                                        }
                                    }
                                }
                                #endregion
                                #region recordType == 1042
                                else if (recordType == 1042) {
                                    if (jobIdSplited.Length == 1) {
                                        PrinterLog updatePrinterLog = LstPrinterLog.FirstOrDefault(f => f.JobId == jobId && f.CycleTime == cycleTime);
                                        if (updatePrinterLog == null) {
                                            LstPrinterLog.Add(new PrinterLog() {
                                                PrinterId = printerId,
                                                JobId = jobId,
                                                CycleTime = cycleTime,
                                                RawData = line,
                                                TotalSheets = value[3].ToInt(),
                                                WidthPage = value[4].ToInt(),
                                                LengthPage = value[5].ToInt(),
                                                DateCreated = dateTimeNow,
                                                ImportId = newImport.ImportId,
                                                LastRecordType = recordType,
                                            });
                                            newImport.CountJob++;
                                        } else {
                                            updatePrinterLog.RawData += line;
                                            updatePrinterLog.TotalSheets = value[3].ToInt();
                                            updatePrinterLog.WidthPage = value[4].ToInt();
                                            updatePrinterLog.LengthPage = value[5].ToInt();
                                            updatePrinterLog.LastRecordType = recordType;
                                            updatePrinterLog.DateModified = dateTimeNow;
                                        }
                                    } else {
                                        int fileId = jobIdSplited[1].ToInt();
                                        PrinterLog updatePrinterLog = LstPrinterLog.FirstOrDefault(f => f.JobId == jobId && f.CycleTime == cycleTime);
                                        if (updatePrinterLog == null) {
                                            LstPrinterLog.Add(new PrinterLog() {
                                                PrinterId = printerId,
                                                JobId = jobId,
                                                CycleTime = cycleTime,
                                                RawData = line,
                                                TotalSheets = value[3].ToInt(),
                                                WidthPage = value[4].ToInt(),
                                                LengthPage = value[5].ToInt(),
                                                DateCreated = dateTimeNow,
                                                ImportId = newImport.ImportId,
                                                LastRecordType = recordType,
                                            });
                                            newImport.CountJob++;
                                        } else {
                                            updatePrinterLog.RawData += line;
                                            updatePrinterLog.TotalSheets = value[3].ToInt();
                                            updatePrinterLog.WidthPage = value[4].ToInt();
                                            updatePrinterLog.LengthPage = value[5].ToInt();
                                            updatePrinterLog.LastRecordType = recordType;
                                            updatePrinterLog.DateModified = dateTimeNow;
                                        }

                                        PrinterLogDetail updatePrinterLogDetail = LstPrinterLogDetail.FirstOrDefault(f => f.JobId == jobId && f.FileId == fileId && f.CycleTime == cycleTime);
                                        if (updatePrinterLogDetail == null) {
                                            LstPrinterLogDetail.Add(new PrinterLogDetail() {
                                                PrinterId = printerId,
                                                JobId = jobId,
                                                FileId = fileId,
                                                CycleTime = cycleTime,
                                                RawData = line,
                                                TotalSheets = value[3].ToInt(),
                                                WidthPage = value[4].ToInt(),
                                                LengthPage = value[5].ToInt(),
                                                DateCreated = dateTimeNow,
                                                ImportId = newImport.ImportId,
                                                LastRecordType = recordType,
                                            });
                                            newImport.CountJobDetail++;
                                        } else {
                                            updatePrinterLogDetail.RawData += line;
                                            updatePrinterLogDetail.TotalSheets = value[3].ToInt();
                                            updatePrinterLogDetail.WidthPage = value[4].ToInt();
                                            updatePrinterLogDetail.LengthPage = value[5].ToInt();
                                            updatePrinterLogDetail.ImportIdModified = newImport.ImportId;
                                            updatePrinterLogDetail.DateModified = dateTimeNow;
                                            updatePrinterLogDetail.LastRecordType = recordType;
                                        }
                                    }
                                }
                                #endregion
                                #region recordType == 1043
                                else if (recordType == 1043) {
                                    if (jobIdSplited.Length == 1) {
                                        PrinterLog updatePrinterLog = LstPrinterLog.FirstOrDefault(f => f.JobId == jobId && f.CycleTime == cycleTime);
                                        if (updatePrinterLog == null) {
                                            LstPrinterLog.Add(new PrinterLog() {
                                                PrinterId = printerId,
                                                JobId = jobId,
                                                CycleTime = cycleTime,
                                                RawData = line,
                                                TotalSheets = value[3].ToInt(),
                                                DateCreated = dateTimeNow,
                                                ImportId = newImport.ImportId,
                                                LastRecordType = recordType,
                                            });
                                            newImport.CountJob++;
                                        } else {
                                            updatePrinterLog.RawData += line;
                                            updatePrinterLog.TotalSheets = value[3].ToInt();
                                            updatePrinterLog.LastRecordType = recordType;
                                            updatePrinterLog.DateModified = dateTimeNow;
                                        }
                                    } else {
                                        int fileId = jobIdSplited[1].ToInt();
                                        PrinterLog updatePrinterLog = LstPrinterLog.FirstOrDefault(f => f.JobId == jobId && f.CycleTime == cycleTime);
                                        if (updatePrinterLog == null) {
                                            LstPrinterLog.Add(new PrinterLog() {
                                                PrinterId = printerId,
                                                JobId = jobId,
                                                CycleTime = cycleTime,
                                                RawData = line,
                                                TotalSheets = value[3].ToInt(),
                                                DateCreated = dateTimeNow,
                                                ImportId = newImport.ImportId,
                                                LastRecordType = recordType,
                                            });
                                            newImport.CountJob++;
                                        } else {
                                            updatePrinterLog.RawData += line;
                                            updatePrinterLog.TotalSheets = value[3].ToInt();
                                            updatePrinterLog.LastRecordType = recordType;
                                            updatePrinterLog.DateModified = dateTimeNow;
                                        }

                                        PrinterLogDetail updatePrinterLogDetail = LstPrinterLogDetail.FirstOrDefault(f => f.JobId == jobId && f.FileId == fileId && f.CycleTime == cycleTime);
                                        if (updatePrinterLogDetail == null) {
                                            LstPrinterLogDetail.Add(new PrinterLogDetail() {
                                                PrinterId = printerId,
                                                JobId = jobId,
                                                FileId = fileId,
                                                CycleTime = cycleTime,
                                                RawData = line,
                                                TotalSheets = value[3].ToInt(),
                                                DateCreated = dateTimeNow,
                                                ImportId = newImport.ImportId,
                                                LastRecordType = recordType,
                                            });
                                            newImport.CountJobDetail++;
                                        } else {
                                            updatePrinterLogDetail.RawData += line;
                                            updatePrinterLogDetail.TotalSheets = value[3].ToInt();
                                            updatePrinterLogDetail.ImportIdModified = newImport.ImportId;
                                            updatePrinterLogDetail.DateModified = dateTimeNow;
                                            updatePrinterLogDetail.LastRecordType = recordType;
                                        }
                                    }
                                }
                                #endregion

                                #region recordType == 1052
                                else if (recordType == 1052) {
                                    if (jobIdSplited.Length == 1) {
                                        PrinterLog updatePrinterLog = LstPrinterLog.FirstOrDefault(f => f.JobId == jobId && f.CycleTime == cycleTime);
                                        if (updatePrinterLog == null) {
                                            LstPrinterLog.Add(new PrinterLog() {
                                                PrinterId = printerId,
                                                JobId = jobId,
                                                CycleTime = cycleTime,
                                                RawData = line,
                                                DateStart = DateTime.ParseExact(value[2] + " " + value[3], "dd.MM.yyyy HH:mm:ss", CultureInfoHelper.CultureInfoEN),
                                                DateEnd = DateTime.ParseExact(value[4] + " " + value[5], "dd.MM.yyyy HH:mm:ss", CultureInfoHelper.CultureInfoEN),
                                                Simplex = value[6].ToBoolean(),
                                                Duplex = value[7].ToBoolean(),
                                                StatusExplorer = value[9].ToInt(),
                                                TotalPage = value[10].ToInt(),
                                                DateCreated = dateTimeNow,
                                                ImportId = newImport.ImportId,
                                                LastRecordType = recordType,
                                            });
                                            newImport.CountJob++;
                                        } else {
                                            updatePrinterLog.RawData += line;
                                            updatePrinterLog.DateStart = DateTime.ParseExact(value[2] + " " + value[3], "dd.MM.yyyy HH:mm:ss", CultureInfoHelper.CultureInfoEN);
                                            updatePrinterLog.DateEnd = DateTime.ParseExact(value[4] + " " + value[5], "dd.MM.yyyy HH:mm:ss", CultureInfoHelper.CultureInfoEN);
                                            updatePrinterLog.Simplex = value[6].ToBoolean();
                                            updatePrinterLog.Duplex = value[7].ToBoolean();
                                            updatePrinterLog.StatusExplorer = value[9].ToInt();
                                            updatePrinterLog.TotalPage = value[10].ToInt();
                                            updatePrinterLog.LastRecordType = recordType;
                                            updatePrinterLog.DateModified = dateTimeNow;
                                        }
                                    } else {
                                        int fileId = jobIdSplited[1].ToInt();
                                        PrinterLog updatePrinterLog = LstPrinterLog.FirstOrDefault(f => f.JobId == jobId && f.CycleTime == cycleTime);
                                        if (updatePrinterLog == null) {
                                            LstPrinterLog.Add(new PrinterLog() {
                                                PrinterId = printerId,
                                                JobId = jobId,
                                                CycleTime = cycleTime,
                                                RawData = line,
                                                DateStart = DateTime.ParseExact(value[2] + " " + value[3], "dd.MM.yyyy HH:mm:ss", CultureInfoHelper.CultureInfoEN),
                                                DateEnd = DateTime.ParseExact(value[4] + " " + value[5], "dd.MM.yyyy HH:mm:ss", CultureInfoHelper.CultureInfoEN),
                                                Simplex = value[6].ToBoolean(),
                                                Duplex = value[7].ToBoolean(),
                                                StatusExplorer = value[9].ToInt(),
                                                TotalPage = value[10].ToInt(),
                                                DateCreated = dateTimeNow,
                                                ImportId = newImport.ImportId,
                                                LastRecordType = recordType,
                                            });
                                            newImport.CountJob++;
                                        } else {
                                            updatePrinterLog.RawData += line;
                                            updatePrinterLog.DateStart = DateTime.ParseExact(value[2] + " " + value[3], "dd.MM.yyyy HH:mm:ss", CultureInfoHelper.CultureInfoEN);
                                            updatePrinterLog.DateEnd = DateTime.ParseExact(value[4] + " " + value[5], "dd.MM.yyyy HH:mm:ss", CultureInfoHelper.CultureInfoEN);
                                            updatePrinterLog.Simplex = value[6].ToBoolean();
                                            updatePrinterLog.Duplex = value[7].ToBoolean();
                                            updatePrinterLog.StatusExplorer = value[9].ToInt();
                                            updatePrinterLog.TotalPage = value[10].ToInt();
                                            updatePrinterLog.LastRecordType = recordType;
                                            updatePrinterLog.DateModified = dateTimeNow;
                                        }

                                        PrinterLogDetail updatePrinterLogDetail = LstPrinterLogDetail.FirstOrDefault(f => f.JobId == jobId && f.FileId == fileId && f.CycleTime == cycleTime);
                                        if (updatePrinterLogDetail == null) {
                                            LstPrinterLogDetail.Add(new PrinterLogDetail() {
                                                PrinterId = printerId,
                                                JobId = jobId,
                                                FileId = fileId,
                                                CycleTime = cycleTime,
                                                RawData = line,
                                                DateStart = DateTime.ParseExact(value[2] + " " + value[3], "dd.MM.yyyy HH:mm:ss", CultureInfoHelper.CultureInfoEN),
                                                DateEnd = DateTime.ParseExact(value[4] + " " + value[5], "dd.MM.yyyy HH:mm:ss", CultureInfoHelper.CultureInfoEN),
                                                Simplex = value[6].ToBoolean(),
                                                Duplex = value[7].ToBoolean(),
                                                StatusExplorer = value[9].ToInt(),
                                                TotalPage = value[10].ToInt(),
                                                DateCreated = dateTimeNow,
                                                ImportId = newImport.ImportId,
                                                LastRecordType = recordType,
                                            });
                                            newImport.CountJobDetail++;
                                        } else {
                                            updatePrinterLogDetail.RawData += line;
                                            updatePrinterLogDetail.DateStart = DateTime.ParseExact(value[2] + " " + value[3], "dd.MM.yyyy HH:mm:ss", CultureInfoHelper.CultureInfoEN);
                                            updatePrinterLogDetail.DateEnd = DateTime.ParseExact(value[4] + " " + value[5], "dd.MM.yyyy HH:mm:ss", CultureInfoHelper.CultureInfoEN);
                                            updatePrinterLogDetail.Simplex = value[6].ToBoolean();
                                            updatePrinterLogDetail.Duplex = value[7].ToBoolean();
                                            updatePrinterLogDetail.StatusExplorer = value[9].ToInt();
                                            updatePrinterLogDetail.TotalPage = value[10].ToInt();
                                            updatePrinterLogDetail.ImportIdModified = newImport.ImportId;
                                            updatePrinterLogDetail.DateModified = dateTimeNow;
                                            updatePrinterLogDetail.LastRecordType = recordType;
                                        }
                                    }
                                }
                                #endregion

                                #region recordType == 1130
                                else if (recordType == 1130) {
                                    PrinterLog updatePrinterLog = LstPrinterLog.FirstOrDefault(f => f.JobId == jobId && f.CycleTime == cycleTime);
                                    if (updatePrinterLog == null) {
                                        LstPrinterLog.Add(new PrinterLog() {
                                            PrinterId = printerId,
                                            JobId = jobId,
                                            CycleTime = cycleTime,
                                            RawData = line,
                                            Client = value[2],
                                            JobName = value[3],
                                            DateSubmission = DateTime.ParseExact(value[8] + " " + value[9], "dd.MM.yyyy HH:mm:ss", CultureInfoHelper.CultureInfoEN),
                                            DateCreated = dateTimeNow,
                                            ImportId = newImport.ImportId,
                                            LastRecordType = recordType,
                                        });
                                        newImport.CountJob++;
                                    } else {
                                        updatePrinterLog.RawData += line;
                                        updatePrinterLog.Client = value[2];
                                        updatePrinterLog.JobName = value[3];
                                        updatePrinterLog.DateSubmission = DateTime.ParseExact(value[8] + " " + value[9], "dd.MM.yyyy HH:mm:ss", CultureInfoHelper.CultureInfoEN);
                                        updatePrinterLog.LastRecordType = recordType;
                                        updatePrinterLog.DateModified = dateTimeNow;
                                    }
                                }
                                #endregion
                            }

                            dbContext.SaveChanges();

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
