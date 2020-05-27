using Exceptionless;
using Exceptionless.Extensions;
using Microsoft.EntityFrameworkCore;
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
    public class Prisma : IDisposable {
        private const int BatchSize = 200;
        private readonly PrintlogDbContext dbContext;

        public Prisma(PrintlogDbContext dbContext) {
            this.dbContext = dbContext;
        }

        public void Dispose() {
            dbContext.Dispose();
            GC.SuppressFinalize(this);
        }

        public void ReadLog(int printerId) {
            var printer = dbContext.MasterTypes.FirstOrDefault(f => f.TypeId == printerId);
            if (printer != null) {
                string printerName = printer.TypeName;
                int cycleTime = printer.CycleTime.Value;

                string pathInput = Path.Combine(Configs.RootPath, @"Inbox", printerName);
                string pathOutputSuccess = Path.Combine(Configs.RootPath, @"Outbox\Successfully", printerName);
                string pathOutputFailed = Path.Combine(Configs.RootPath, @"Outbox\Failed", printerName);

                if (!Directory.Exists(pathOutputSuccess)) {
                    Directory.CreateDirectory(pathOutputSuccess);
                }

                if (!Directory.Exists(pathOutputFailed)) {
                    Directory.CreateDirectory(pathOutputFailed);
                }

                int rowCount;
                foreach (string file in Directory.EnumerateFiles(pathInput, "*.acc")) {
                    DateTime dateTimeNow = DateTime.Now;
                    #region Add ImportFile
                    ImportFile newImport = new ImportFile() {
                        PrinterId = printerId,
                        FileName = Path.GetFileName(file),
                        CountLine = 0,
                        CountJob = 0,
                        CountJobDetail = 0,
                        IsSuccess = false,
                        DateCreated = dateTimeNow,
                    };
                    #endregion
                    dbContext.ImportFiles.Add(newImport);
                    int rnt = dbContext.SaveChanges();

                    List<PrinterLog> LstPrinterLog = new List<PrinterLog>();
                    List<PrinterLogDetail> LstPrinterLogDetail = new List<PrinterLogDetail>();
                    using (StreamReader reader = new StreamReader(file)) {
                        try {
                            while (!reader.EndOfStream) {
                                string line = reader.ReadLine();
                                newImport.CountLine++;

                                string[] value = line.Split(';');
                                string[] jobIdSplited = value[1].Split('.');
                                string jobId = jobIdSplited[0];
                                int recordType = value[0].ToInt();

                                if (jobId == "00000001") {
                                    cycleTime = printer.CycleTime.Value + 1;
                                }

                                #region recordType == 1000
                                if (recordType == 1000) {
                                    PrinterLog updatePrinterLog = LstPrinterLog.FirstOrDefault(f => f.JobId == jobId && f.CycleTime == cycleTime);
                                    if (updatePrinterLog == null) {
                                        LstPrinterLog.Add(new PrinterLog() {
                                            PrinterId = printerId,
                                            JobId = jobId,
                                            CycleTime = cycleTime,
                                            DateCreated = dateTimeNow,
                                            ImportId = newImport.ImportId,
                                            LastRecordType = recordType,
                                        });
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
                                            DateCreated = dateTimeNow,
                                            ImportId = newImport.ImportId,
                                            LastRecordType = recordType,
                                        });
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
                                                JobType = value[2],
                                                FullPath = value[3],
                                                JobName = Path.GetFileName(value[3]),
                                                Filesize = value[4].ToInt64(),
                                                Copies = value[5].ToInt(),
                                                Formdef = value[6],
                                                Pagedef = value[7],
                                                DateCreated = dateTimeNow,
                                                ImportId = newImport.ImportId,
                                                LastRecordType = recordType,
                                            });
                                            newImport.CountJobDetail++;
                                        } else {
                                            updatePrinterLogDetail.JobType = value[2];
                                            updatePrinterLogDetail.FullPath = value[3];
                                            updatePrinterLogDetail.JobName = Path.GetFileName(value[3]);
                                            updatePrinterLogDetail.Filesize = value[4].ToInt64();
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
                                            UserExplorer = value[2],
                                            StatusExplorer = value[3].ToInt(),
                                            DateExplorer = DateTime.ParseExact(value[4] + " " + value[5], "dd.MM.yyyy HH:mm:ss", CultureInfoHelper.CultureInfoEN),
                                            DateCreated = dateTimeNow,
                                            ImportId = newImport.ImportId,
                                            LastRecordType = recordType,
                                        });
                                    } else {
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
                                            DateCreated = dateTimeNow,
                                            ImportId = newImport.ImportId,
                                            LastRecordType = recordType,
                                        });
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
                                        } else {
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
                                        } else {
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
                                        } else {
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
                                        } else {
                                            
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

                                            DateCreated = dateTimeNow,
                                            ImportId = newImport.ImportId,
                                            LastRecordType = recordType,
                                        });
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

                                            DateCreated = dateTimeNow,
                                            ImportId = newImport.ImportId,
                                            LastRecordType = recordType,
                                        });
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
                                        } else {
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
                                        } else {

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
                                        } else {
                                            
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
    
                                                DateCreated = dateTimeNow,
                                                ImportId = newImport.ImportId,
                                                LastRecordType = recordType,
                                            });
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
    
                                                DateCreated = dateTimeNow,
                                                ImportId = newImport.ImportId,
                                                LastRecordType = recordType,
                                            });
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
    
                                                TotalSheets = value[3].ToInt(),
                                                WidthPage = value[4].ToInt(),
                                                LengthPage = value[5].ToInt(),
                                                DateCreated = dateTimeNow,
                                                ImportId = newImport.ImportId,
                                                LastRecordType = recordType,
                                            });
                                        } else {

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
    
                                                TotalSheets = value[3].ToInt(),
                                                WidthPage = value[4].ToInt(),
                                                LengthPage = value[5].ToInt(),
                                                DateCreated = dateTimeNow,
                                                ImportId = newImport.ImportId,
                                                LastRecordType = recordType,
                                            });
                                        } else {

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
    
                                                TotalSheets = value[3].ToInt(),
                                                WidthPage = value[4].ToInt(),
                                                LengthPage = value[5].ToInt(),
                                                DateCreated = dateTimeNow,
                                                ImportId = newImport.ImportId,
                                                LastRecordType = recordType,
                                            });
                                        } else {
                                            
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
    
                                                TotalSheets = value[3].ToInt(),
                                                DateCreated = dateTimeNow,
                                                ImportId = newImport.ImportId,
                                                LastRecordType = recordType,
                                            });
                                        } else {

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
    
                                                TotalSheets = value[3].ToInt(),
                                                DateCreated = dateTimeNow,
                                                ImportId = newImport.ImportId,
                                                LastRecordType = recordType,
                                            });
                                        } else {

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
    
                                                TotalSheets = value[3].ToInt(),
                                                DateCreated = dateTimeNow,
                                                ImportId = newImport.ImportId,
                                                LastRecordType = recordType,
                                            });
                                        } else {
                                            
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
                                        } else {

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
                                        } else {

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
                                        } else {
                                            
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

                                            Client = value[2],
                                            JobName = value[3],
                                            DateSubmission = DateTime.ParseExact(value[8] + " " + value[9], "dd.MM.yyyy HH:mm:ss", CultureInfoHelper.CultureInfoEN),
                                            DateCreated = dateTimeNow,
                                            ImportId = newImport.ImportId,
                                            LastRecordType = recordType,
                                        });
                                    } else {

                                        updatePrinterLog.Client = value[2];
                                        updatePrinterLog.JobName = value[3];
                                        updatePrinterLog.DateSubmission = DateTime.ParseExact(value[8] + " " + value[9], "dd.MM.yyyy HH:mm:ss", CultureInfoHelper.CultureInfoEN);
                                        updatePrinterLog.LastRecordType = recordType;
                                        updatePrinterLog.DateModified = dateTimeNow;
                                    }
                                }
                                #endregion
                            }
                            reader.Close();
                            rowCount = 0;
                            var printerLogs = dbContext.PrinterLogs.AsNoTracking().Where(w => w.PrinterId == printerId).ToList();
                            foreach (var printerLog in LstPrinterLog) {
                                var printerLogInfo = printerLogs.FirstOrDefault(f => f.JobId == printerLog.JobId && f.CycleTime == printerLog.CycleTime);
                                if (printerLogInfo != null) {
                                    #region Update PrinterLogs
                                    //dbContext.PrinterLogs.Attach(printerLogInfo);
                                    printerLogInfo.Client = printerLog.Client ?? printerLogInfo.Client;
                                    printerLogInfo.Sender = printerLog.Sender ?? printerLogInfo.Sender;
                                    printerLogInfo.Printer = printerLog.Printer ?? printerLogInfo.Printer;
                                    printerLogInfo.Jobqueue = printerLog.Jobqueue ?? printerLogInfo.Jobqueue;
                                    printerLogInfo.Resolution = printerLog.Resolution ?? printerLogInfo.Resolution;
                                    printerLogInfo.Proofprint = printerLog.Proofprint ?? printerLogInfo.Proofprint;
                                    printerLogInfo.Storeprint = printerLog.Storeprint ?? printerLogInfo.Storeprint;
                                    printerLogInfo.CustAcc = printerLog.CustAcc ?? printerLogInfo.CustAcc;
                                    printerLogInfo.ReferenceId = printerLog.ReferenceId ?? printerLogInfo.ReferenceId;
                                    printerLogInfo.Form = printerLog.Form ?? printerLogInfo.Form;
                                    printerLogInfo.TicketName = printerLog.TicketName ?? printerLogInfo.TicketName;
                                    printerLogInfo.JobName = printerLog.JobName ?? printerLogInfo.JobName;
                                    printerLogInfo.OrderId = printerLog.OrderId ?? printerLogInfo.OrderId;
                                    printerLogInfo.Range = printerLog.Range ?? printerLogInfo.Range;
                                    printerLogInfo.ColorIds = printerLog.ColorIds ?? printerLogInfo.ColorIds;
                                    printerLogInfo.TrackingEnabled = printerLog.TrackingEnabled ?? printerLogInfo.TrackingEnabled;
                                    printerLogInfo.TotalFile = printerLog.TotalFile ?? printerLogInfo.TotalFile;
                                    printerLogInfo.UserExplorer = printerLog.UserExplorer ?? printerLogInfo.UserExplorer;
                                    printerLogInfo.StatusExplorer = printerLog.StatusExplorer ?? printerLogInfo.StatusExplorer;
                                    printerLogInfo.JobType = printerLog.JobType ?? printerLogInfo.JobType;
                                    printerLogInfo.Simplex = printerLog.Simplex ?? printerLogInfo.Simplex;
                                    printerLogInfo.Duplex = printerLog.Duplex ?? printerLogInfo.Duplex;
                                    printerLogInfo.TotalPage = printerLog.TotalPage ?? printerLogInfo.TotalPage;
                                    printerLogInfo.OriginalPages = printerLog.OriginalPages ?? printerLogInfo.OriginalPages;
                                    printerLogInfo.FrontPages = printerLog.FrontPages ?? printerLogInfo.FrontPages;
                                    printerLogInfo.BackPages = printerLog.BackPages ?? printerLogInfo.BackPages;
                                    printerLogInfo.TotalSheets = printerLog.TotalSheets ?? printerLogInfo.TotalSheets;
                                    printerLogInfo.InformationPages = printerLog.InformationPages ?? printerLogInfo.InformationPages;
                                    printerLogInfo.InformationSheets = printerLog.InformationSheets ?? printerLogInfo.InformationSheets;
                                    printerLogInfo.TotalOffsets = printerLog.TotalOffsets ?? printerLogInfo.TotalOffsets;
                                    printerLogInfo.Feet = printerLog.Feet ?? printerLogInfo.Feet;
                                    printerLogInfo.Nup = printerLog.Nup ?? printerLogInfo.Nup;
                                    printerLogInfo.WidthPage = printerLog.WidthPage ?? printerLogInfo.WidthPage;
                                    printerLogInfo.LengthPage = printerLog.LengthPage ?? printerLogInfo.LengthPage;
                                    printerLogInfo.DateSubmission = printerLog.DateSubmission ?? printerLogInfo.DateSubmission;
                                    printerLogInfo.DateExplorer = printerLog.DateExplorer ?? printerLogInfo.DateExplorer;
                                    printerLogInfo.DateStartQueue = printerLog.DateStartQueue ?? printerLogInfo.DateStartQueue;
                                    printerLogInfo.DateEndQueue = printerLog.DateEndQueue ?? printerLogInfo.DateEndQueue;
                                    printerLogInfo.DateStart = printerLog.DateStart ?? printerLogInfo.DateStart;
                                    printerLogInfo.DateEnd = printerLog.DateEnd ?? printerLogInfo.DateEnd;
                                    printerLogInfo.ImportIdModified = printerLog.ImportIdModified;
                                    printerLogInfo.DateModified = printerLog.DateModified;
                                    printerLogInfo.LastRecordType = printerLog.LastRecordType;
                                    dbContext.Entry(printerLogInfo).State = EntityState.Modified;
                                    #endregion
                                } else {
                                    #region Add PrinterLogs
                                    dbContext.PrinterLogs.Add(printerLog);
                                    newImport.CountJob++;
                                    #endregion
                                }
                                rowCount++;
                                if (rowCount % BatchSize == 0) {
                                    var rowAffeced = dbContext.SaveChanges();
                                }
                            }
                            printerLogs.Clear();
                            printerLogs.TrimExcess();

                            var printerLogDetails = dbContext.PrinterLogDetails.AsNoTracking().Where(w => w.PrinterId == printerId).ToList();
                            rowCount = 0;
                            foreach (var printerLogDetail in LstPrinterLogDetail) {
                                var printerLogDetailInfo = printerLogDetails.FirstOrDefault(f => f.JobId == printerLogDetail.JobId && f.CycleTime == printerLogDetail.CycleTime && f.FileId == printerLogDetail.FileId);
                                if (printerLogDetailInfo != null) {
                                    #region Update PrinterLogDetails
                                    //dbContext.PrinterLogDetails.Attach(printerLogDetailInfo);
                                    printerLogDetailInfo.JobType = printerLogDetail.JobType ?? printerLogDetailInfo.JobType;
                                    printerLogDetailInfo.FullPath = printerLogDetail.FullPath ?? printerLogDetailInfo.FullPath;
                                    printerLogDetailInfo.JobName = printerLogDetail.JobName ?? printerLogDetailInfo.JobName;
                                    printerLogDetailInfo.Filesize = printerLogDetail.Filesize ?? printerLogDetailInfo.Filesize;
                                    printerLogDetailInfo.Copies = printerLogDetail.Copies ?? printerLogDetailInfo.Copies;
                                    printerLogDetailInfo.Formdef = printerLogDetail.Formdef ?? printerLogDetailInfo.Formdef;
                                    printerLogDetailInfo.Pagedef = printerLogDetail.Pagedef ?? printerLogDetailInfo.Pagedef;
                                    printerLogDetailInfo.UserExplorer = printerLogDetail.UserExplorer ?? printerLogDetailInfo.UserExplorer;
                                    printerLogDetailInfo.StatusExplorer = printerLogDetail.StatusExplorer ?? printerLogDetailInfo.StatusExplorer;
                                    printerLogDetailInfo.Printer = printerLogDetail.Printer ?? printerLogDetailInfo.Printer;
                                    printerLogDetailInfo.Jobqueue = printerLogDetail.Jobqueue ?? printerLogDetailInfo.Jobqueue;
                                    printerLogDetailInfo.Range = printerLogDetail.Range ?? printerLogDetailInfo.Range;
                                    printerLogDetailInfo.Form = printerLogDetail.Form ?? printerLogDetailInfo.Form;
                                    printerLogDetailInfo.Simplex = printerLogDetail.Simplex ?? printerLogDetailInfo.Simplex;
                                    printerLogDetailInfo.Duplex = printerLogDetail.Duplex ?? printerLogDetailInfo.Duplex;
                                    printerLogDetailInfo.TotalPage = printerLogDetail.TotalPage ?? printerLogDetailInfo.TotalPage;
                                    printerLogDetailInfo.OriginalPages = printerLogDetail.OriginalPages ?? printerLogDetailInfo.OriginalPages;
                                    printerLogDetailInfo.FrontPages = printerLogDetail.FrontPages ?? printerLogDetailInfo.FrontPages;
                                    printerLogDetailInfo.BackPages = printerLogDetail.BackPages ?? printerLogDetailInfo.BackPages;
                                    printerLogDetailInfo.TotalSheets = printerLogDetail.TotalSheets ?? printerLogDetailInfo.TotalSheets;
                                    printerLogDetailInfo.InformationPages = printerLogDetail.InformationPages ?? printerLogDetailInfo.InformationPages;
                                    printerLogDetailInfo.InformationSheets = printerLogDetail.InformationSheets ?? printerLogDetailInfo.InformationSheets;
                                    printerLogDetailInfo.TotalOffsets = printerLogDetail.TotalOffsets ?? printerLogDetailInfo.TotalOffsets;
                                    printerLogDetailInfo.Feet = printerLogDetail.Feet ?? printerLogDetailInfo.Feet;
                                    printerLogDetailInfo.Nup = printerLogDetail.Nup ?? printerLogDetailInfo.Nup;
                                    printerLogDetailInfo.WidthPage = printerLogDetail.WidthPage ?? printerLogDetailInfo.WidthPage;
                                    printerLogDetailInfo.LengthPage = printerLogDetail.LengthPage ?? printerLogDetailInfo.LengthPage;
                                    printerLogDetailInfo.DateSubmission = printerLogDetail.DateSubmission ?? printerLogDetailInfo.DateSubmission;
                                    printerLogDetailInfo.DateStartQueue = printerLogDetail.DateStartQueue ?? printerLogDetailInfo.DateStartQueue;
                                    printerLogDetailInfo.DateEndQueue = printerLogDetail.DateEndQueue ?? printerLogDetailInfo.DateEndQueue;
                                    printerLogDetailInfo.DateStart = printerLogDetail.DateStart ?? printerLogDetailInfo.DateStart;
                                    printerLogDetailInfo.DateEnd = printerLogDetail.DateEnd ?? printerLogDetailInfo.DateEnd;
                                    printerLogDetailInfo.ImportIdModified = printerLogDetail.ImportIdModified;
                                    printerLogDetailInfo.DateModified = printerLogDetail.DateModified;
                                    printerLogDetailInfo.LastRecordType = printerLogDetail.LastRecordType;
                                    dbContext.Entry(printerLogDetailInfo).State = EntityState.Modified;
                                    #endregion
                                } else {
                                    #region Add PrinterLogDetails
                                    dbContext.PrinterLogDetails.Add(printerLogDetail);
                                    newImport.CountJobDetail++;
                                    #endregion
                                }
                                rowCount++;
                                if (rowCount % BatchSize == 0) {
                                    var rowAffeced = dbContext.SaveChanges();
                                }
                            }
                            printerLogDetails.Clear();
                            printerLogDetails.TrimExcess();

                            printer.CycleTime = cycleTime;
                            newImport.IsSuccess = true;

                            dbContext.SaveChanges();
                            dbContext.Dispose();

                            #region Move Input File to Success
                            string pathSuccess = Path.Combine(pathOutputSuccess, Path.GetFileName(file));
                            int runningNo = 2;
                            while (File.Exists(pathSuccess)) {
                                pathSuccess = Path.Combine(pathOutputSuccess, Path.GetFileNameWithoutExtension(file) + "_v" + runningNo + Path.GetExtension(file));
                                runningNo++;
                            }
                            File.Copy(file, pathSuccess);
                            File.Delete(file);
                            #endregion
                        } catch (Exception ex) {
                            dbContext.Dispose();
                            reader.Close();
                            ex.ToExceptionless().AddObject(printerId, "printerId").AddObject(file, "file").AddObject(newImport, "newImport").Submit();

                            #region Move Input File to Failed
                            string pathFailed = Path.Combine(pathOutputFailed, Path.GetFileName(file));
                            int runningNo = 2;
                            while (File.Exists(pathFailed)) {
                                pathFailed = Path.Combine(pathOutputFailed, Path.GetFileNameWithoutExtension(file) + "_v" + runningNo + Path.GetExtension(file));
                                runningNo++;
                            }
                            File.Copy(file, pathFailed);
                            File.Delete(file);
                            #endregion
                        }
                    }
                }
            }
        }
    }
}
