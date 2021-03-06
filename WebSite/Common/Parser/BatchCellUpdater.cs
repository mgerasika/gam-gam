using System;
using System.Collections.Generic;
using System.Diagnostics;
using Google.GData.Client;
using Google.GData.Spreadsheets;

namespace FoodApp.Common.Parser {
    public class BatchCellUpdater {
        private static bool RequestUpdateCells(List<ExcelCell> cells) {
            bool res = false;

            if (cells.Count > 0) {
                ExcelTable excelTable = cells[0].GetRow().GetTable();
                CellFeed cellFeed = excelTable.GetCellFeed();
                CellQuery cellQuery = new CellQuery(excelTable.GetEntry().CellFeedLink);

                Dictionary<string, CellEntry> cellEntries = CreateEntryCellsMap(ExcelManager.Inst.SpreadsheetsService,
                    cellFeed, cells);
                CellFeed batchRequest = new CellFeed(cellQuery.Uri, ExcelManager.Inst.SpreadsheetsService);
                
                foreach (ExcelCell cell in cells) {
                    CellEntry batchEntry = cellEntries[cell.GetBatchID()];
                    string inputValue = "";
                    if (cell.EditTmpValue > 0) {
                        inputValue = cell.EditTmpValue.ToString().Replace(".", ",");
                    }
                    batchEntry.InputValue = inputValue;
                    batchEntry.BatchData = new GDataBatchEntryData(cell.GetBatchID(), GDataBatchOperationType.update);
                    batchRequest.Entries.Add(batchEntry);
                }
                // Submit the update
                CellFeed batchResponse =
                    (CellFeed) ExcelManager.Inst.SpreadsheetsService.Batch(batchRequest, new Uri(cellFeed.Batch));

                foreach (CellEntry entry in batchResponse.Entries) {
                    string batchId = entry.BatchData.Id;

                    if (entry.BatchData.Status.Code != 200 ) {
                        GDataBatchStatus status = entry.BatchData.Status;
                        Console.WriteLine("{0} failed ({1})", batchId, status.Reason);
                    }
                    else {
                        ExcelCell cell = ExcelRow.GetCellByBatchId(cells, batchId);
                        Debug.Assert(null != cell);
                        cell.Value = cell.EditTmpValue;
                        cell.SetEntry(entry);
                        res = true;
                    }
                }
            }
            return res;
        }

        private static Dictionary<string, CellEntry> CreateEntryCellsMap(SpreadsheetsService service, CellFeed cellFeed,
            List<ExcelCell> cells) {
            Dictionary<string, CellEntry> res = new Dictionary<string, CellEntry>();

            CellFeed batchRequest = new CellFeed(new Uri(cellFeed.Self), service);
            foreach (ExcelCell cell in cells) {
                if (cell.GetEntry() == null) {
                    CellEntry batchEntry = new CellEntry(cell.Row, cell.Column, cell.GetBatchID());
                    batchEntry.Id = new AtomId(string.Format("{0}/{1}", cellFeed.Self, cell.GetBatchID()));
                    batchEntry.BatchData = new GDataBatchEntryData(cell.GetBatchID(), GDataBatchOperationType.query);
                    batchRequest.Entries.Add(batchEntry);
                }
                else {
                    if (!res.ContainsKey(cell.GetBatchID())) {
                        res.Add(cell.GetBatchID(), cell.GetEntry());
                    }
                }
            }

            if (batchRequest.Entries.Count > 0) {
                CellFeed queryBatchResponse = (CellFeed) service.Batch(batchRequest, new Uri(cellFeed.Batch));
                foreach (CellEntry entry in queryBatchResponse.Entries) {
                    res.Add(entry.BatchData.Id, entry);
                }
            }

            return res;
        }

        public static bool Update(ngUserModel user, int day, List<ngOrderEntry> orders) {
            bool res = false;
            ExcelManager.Inst.RefreshAccessToken();
            Dictionary<ngUserModel,List<ngOrderEntry>>  items = new Dictionary<ngUserModel, List<ngOrderEntry>>();
            items.Add(user,orders);
            res = Update(day,items);
            return res;
        }

        public static bool Update(int day, Dictionary<ngUserModel, List<ngOrderEntry>> orders) {
            bool res = false;
            ExcelManager.Inst.RefreshAccessToken();

            ExcelTable table = ExcelManager.Inst.Doc.GetExcelTable(day);
            List<ExcelCell> newCells = new List<ExcelCell>();
            foreach (KeyValuePair<ngUserModel, List<ngOrderEntry>> keyValuePair in orders) {
                foreach (ngOrderEntry order in keyValuePair.Value) {
                    ExcelRow row = table.GetRowByFoodId(order.FoodId);
                    ExcelCell cell = row.EnsureCell(keyValuePair.Key.Column);
                    cell.EditTmpValue = order.Count;
                    newCells.Add(cell);
                }
            }
            res = RequestUpdateCells(newCells);
            return res;
        }
    }
}