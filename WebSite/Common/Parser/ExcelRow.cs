using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using FoodApp.Common.Managers;
using Google.GData.Spreadsheets;

namespace FoodApp.Common.Parser {
    public class ExcelRow {
        private readonly List<CellEntry> _entry;
        private readonly ExcelTable _table;
        public List<ExcelCell> Cells = new List<ExcelCell>();


        public ExcelRow(ExcelTable table, uint row, List<CellEntry> entry) {
            _entry = entry;
            _table = table;
            Row = row;
        }

        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        private uint Row { get; set; }
        public bool HasPrice { get; set; }
        public string OriginalCategory { get; set; }
        public string Category { get; set; }

        public ExcelTable GetTable() {
            return _table;
        }

        internal ExcelCell GetCell(uint column) {
            ExcelCell res = null;

            foreach (ExcelCell cell in Cells) {
                if (cell.Column.Equals(column)) {
                    res = cell;
                    break;
                }
            }
            return res;
        }

        internal ExcelCell GetCellByBatchId(string batchId) {
            ExcelCell res = null;

            foreach (ExcelCell cell in Cells) {
                if (cell.GetBatchID().Equals(batchId)) {
                    res = cell;
                    break;
                }
            }
            return res;
        }

        public static ExcelCell GetCellByBatchId(List<ExcelCell> cells, string batchId) {
            ExcelCell res = null;

            foreach (ExcelCell cell in cells) {
                if (cell.GetBatchID().Equals(batchId)) {
                    res = cell;
                    break;
                }
            }
            return res;
        }

        public string GetFoodId() {
            string res = "";
            if (!string.IsNullOrEmpty(OriginalCategory)) {
                res = new Regex(@"\W").Replace(OriginalCategory, "");
            }
            if (!string.IsNullOrEmpty(Name)) {
                res += new Regex(@"\W").Replace(Name, "");
            }
            if (!string.IsNullOrEmpty(res)) {
                res = "_" + res;
                res = ApiUtils.GetLatinCodeFromCyrillic(res);
            }
            return res;
        }


        internal ExcelCell EnsureCell(uint column) {
            ExcelCell cell = GetCell(column);
            if (null == cell) {
                cell = new ExcelCell(this, null);
                cell.Column = column;
                cell.Row = Row;
                Cells.Add(cell);
            }
            return cell;
        }

        private string GetCategory() {
            string res = "";

            bool isCategoryRow = false;
            for (int j = 0; j < _entry.Count; ++j) {
                CellEntry element = _entry[j];
                if (element.Column.Equals(ColumnNames.Price)) {
                    string val = element.Value;
                    if (!string.IsNullOrEmpty(val) && val.ToLower().Contains("����")) {
                        isCategoryRow = true;
                        break;
                    }
                }
                else if (element.Column.Equals(ColumnNames.Description))
                {
                    string val = element.Value;
                    if (!string.IsNullOrEmpty(val) && val.ToLower().Contains("���������"))
                    {
                        isCategoryRow = true;
                        break;
                    }
                }
            }

            if (isCategoryRow) {
                for (int j = 0; j < _entry.Count; ++j) {
                    CellEntry element = _entry[j];
                    if (element.Column.Equals(ColumnNames.Name)) {
                        res = element.Value;
                        break;
                    }
                }
            }
            return res;
        }

        public void Parse(ref string lCategory) {
            string tmpCategory = GetCategory();
            if (!string.IsNullOrEmpty(tmpCategory)) {
                lCategory = tmpCategory.Replace(":", "");
            }
            OriginalCategory = lCategory;

            for (int j = 0; j < _entry.Count; ++j) {
                CellEntry cell = _entry[j];

                if (cell.Column.Equals(ColumnNames.Description)) {
                    Description = cell.Value;
                }
                else if (cell.Column.Equals(ColumnNames.Name)) {
                    Name = cell.Value;
                }
                else if (cell.Column.Equals(ColumnNames.Price)) {
                    string price = cell.Value;
                    if (!string.IsNullOrEmpty(price)) {
                        string str = price;

                        decimal lPrice = 0;
                        if (ApiUtils.TryDecimalParse(str, out lPrice)) {
                            Price = lPrice;
                            HasPrice = true;
                        }
                    }
                }
                else {
                    ExcelCell excelCell = new ExcelCell(this, cell);
                    Cells.Add(excelCell);
                    excelCell.Parse();
                }
            }


            if (OriginalCategory != null && Name != null && OriginalCategory.Contains("���������") &&
                !IsContainer(Name)) {
                Name = OriginalCategory + " " + Name;
            }
        }

        public static string GetNewCategory(string category, string name) {
            string res = category;
            if (category.Equals("������", StringComparison.OrdinalIgnoreCase)) {
                res = CategoryNames.Salat;
            }

            else if (category.Equals("�������", StringComparison.OrdinalIgnoreCase)) {
                res = CategoryNames.Garnir;
            }

            else if (category.Equals("����� ������", StringComparison.OrdinalIgnoreCase)) {
                res = CategoryNames.First;
            }
            else if (IsContainer(name))
            {
                res = CategoryNames.PlactisContainer;
            }
            else if (name != null && name.ToLower().Contains("�����")) {
                res = CategoryNames.Breat;
            }
            else if (category.ToLower().Contains("�����������")) {
                res = CategoryNames.ComplexDinner;
            }
            else if (name != null && name.ToLower().Contains("���������")) {
                res = CategoryNames.Garnir;
            }

            else {
                res = CategoryNames.MeatOrFish;
            }

            return res;
        }

        private static bool IsContainer(string name) {
            return name != null && (name.ToLower().Contains("����������") || name.ToLower().Contains("���������"));
        }

        internal void MergeEntry(CellEntry newEntry) {
            CellEntry old = null;
            foreach (CellEntry obj in _entry) {
                if (obj.Column == newEntry.Column && obj.Row == newEntry.Row) {
                    old = obj;
                    break;
                }
            }

            if (null != old) {
                _entry.Remove(old);
            }
            _entry.Add(newEntry);
        }

        public bool IsByWeightItem() {
            bool res = false;
            if (Category.Equals(CategoryNames.Salat, StringComparison.OrdinalIgnoreCase)) {
                res = true;
            }
            if (Name.Contains("������ ���.��������") ||
                Name.Contains("���.���� � ����") ||
                Name.Contains("��������") ||
                Name.Contains("�������� �������") ||
                Name.Contains("��������") ||
                Name.Contains("�������� ����") ||
                Name.Contains("����� �������") ||
                Name.Contains("������") ||
                Name.Contains("����� �������") ||
                Name.Contains("����� �������") ||
                Name.Contains("����� �������") ||
                Name.Contains("������� � �������")) {
                res = true;
            }

            return res;
        }

        public bool IsFirst() {
            bool res = false;
            if (Category.Equals(CategoryNames.First)) {
                res = true;
            }
            return res;
        }

        public bool IsMeatOrFish() {
            bool res = false;
            if (Category.Equals(CategoryNames.MeatOrFish)) {
                res = true;
            }
            return res;
        }

        public bool IsSalat() {
            bool res = false;
            if (Category.Equals(CategoryNames.Salat)) {
                res = true;
            }
            return res;
        }

        public bool IsGarnir() {
            bool res = false;
            if (Category.Equals(CategoryNames.Garnir)) {
                res = true;
            }
            return res;
        }

        public bool IsKvasolevaOrChanachi() {
            return Name.Contains("���� �����") || Name.Contains("������");
        }

        public bool IsContainer() {
            bool res = IsContainer(this.Name);
            return res;
        }

        public bool IsSmallContainer() {
            bool res = false;
            if (IsContainer(this.Name)&& Name.Contains("1")) {
                res = true;
            }
            return res;
        }

        public bool IsBigContainer() {
            bool res = false;
            if (IsContainer(this.Name) && Name.Contains("2"))
            {
                res = true;
            }
            return res;
        }
    }
}