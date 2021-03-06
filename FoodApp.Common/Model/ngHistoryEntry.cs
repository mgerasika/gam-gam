using System;
using SharpKit.JavaScript;

namespace FoodApp.Common
{
    [JsType(JsMode.Json, Filename = CommonApiResources._fileClientJs, Export = true)]
    public class ngHistoryEntry : ngModelBase
    {
        public string FoodId { get; set; }
        public DateTime Date { get; set; }
        public decimal Count { get; set; }
        public decimal FoodPrice { get; set; }

        public override string ToString() {
            return Date.ToShortDateString();
        }
    }
}