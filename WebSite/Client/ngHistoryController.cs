using angularjs;
using FoodApp.Common;
using FoodApp.Common.Model;
using FoodApp.Common.Url;
using FoodApp.Controllers.api;
using SharpKit.Html;
using SharpKit.jQuery;
using SharpKit.JavaScript;

namespace FoodApp.Client {
    [JsType(JsMode.Prototype, Filename = WebApiResources._fileClientJs)]
    public class ngHistoryController : ngControllerBase {
        public static ngHistoryController inst = new ngHistoryController();

        private ngHistoryController() {
        }

        public override string className {
            get { return "ngHistoryController"; }
        }

        public JsArray<ngHistoryGroupEntry> ngHistoryItems {
            get { return _scope["ngHistoryItems"].As<JsArray<ngHistoryGroupEntry>>(); }
            set { _scope["ngHistoryItems"] = value; }
        }


        public override void init(angularScope scope, angularHttp http, angularLocation loc, angularFilter filter) {
            base.init(scope, http, loc, filter);
            ngHistoryItems = new JsArray<ngHistoryGroupEntry>();

            eventManager.inst.subscribe(eventManager.settingsLoaded, delegate(int n) { refreshHistory(); });

            eventManager.inst.subscribe(eventManager.orderCompleted, delegate(int n) { refreshHistory(); });
        }

        public ngFoodItem getFoodItem(string id) {
            ngFoodItem item = ngFoodController.inst.findFoodById(id);
            return item;
        }

        public void refreshHistory() {
            serviceHlp.inst.SendGet("json",
                HistoryUrl.c_sHistoryPrefix + "/" + ngAppController.inst.ngUserId + "/",
                delegate(object o, JsString s, jqXHR arg3) {
                    ngHistoryItems = o.As<JsArray<ngHistoryGroupEntry>>();

                    _scope.apply();
                }, onRequestFailed);
        }


        public void deleteHistoryClick(ngHistoryGroupEntry group) {
            serviceHlp.inst.SendPost("json",
                HistoryUrl.c_sDeleteHistoryPrefix + "/" + ngAppController.inst.ngUserId + "/",JSON.stringify(group),
                delegate { refreshHistory(); }, onRequestFailed);
        }
    }
}