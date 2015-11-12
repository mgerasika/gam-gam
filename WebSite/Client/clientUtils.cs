using FoodApp.Common;
using SharpKit.Html;
using SharpKit.JavaScript;

namespace FoodApp.Client
{
    [JsType(JsMode.Prototype, Filename = WebApiResources._fileClientJs)]
    public class clientUtils
    {
        public static clientUtils Inst = new clientUtils();

        private clientUtils()
        {
        }

        public string getLocation()
        {
            string name = HtmlContext.document.location.protocol + "//" + HtmlContext.document.location.host;
            return name;
        }

        public void showLoading()
        {
            HtmlElement loadingEl = HtmlContext.document.getElementById("loadingIcon") as HtmlElement;
            loadingEl.style.display = "block";
        }

        public void prettyPrint(string txt, string id)
        {
            JsFunction fn = HtmlContext.window.As<JsObject>()["prettyPrintEx"].As<JsFunction>();
            fn.call(HtmlContext.window, txt, id);
        }

       


        public void hideLoading()
        {
            HtmlContext.window.setTimeout(delegate
            {
                HtmlElement loadingEl = HtmlContext.document.getElementById("loadingIcon") as HtmlElement;
                loadingEl.style.display = "none";
            }, 200);
        }

        public string getSelectedText(Window wnd)
        {
            string text = "";
            if (wnd.As<JsObject>()["getSelection"] != null)
            {
                text = wnd.getSelection().toString();
            }
            else
            {
                JsObject selection = wnd.document.As<JsObject>()["selection"].As<JsObject>();
                if (selection != null && (selection["type"] != "Control"))
                {
                    JsFunction fn = selection["createRange"].As<JsFunction>();
                    text = fn.call().As<JsObject>()["text"].As<JsString>();
                }
            }
            return text;
        }

      

        public bool isEmpty(object str)
        {
            return (null == str) || (JsContext.undefined == str) || ("" == str) || ("null" == str);
        }

        internal bool Contains(JsArray<string> ngCategories, string p) {
            bool res = false;
            foreach (string str in ngCategories) {
                if (str == p) {
                    res = true;
                    break;
                }
            }
            return res;
        }
    }
}