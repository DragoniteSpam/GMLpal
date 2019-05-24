#pragma warning disable 649
namespace GMLpal {
    public class GMS2Project {
        public string Name {
            get; set;
        } = "GMS2 Project";

        public string id = "";
        public string modelName = "";
        public string mvc = "";
        public string IsDnDProject = "";
        public object[] configs = null;
        public string option_ecma = "";
        //public GMS2_AlteredResources[] alteredResources;
        //public GMS2_HiddenResources[] hiddenResources;
        public string projectPath = "";
        public GMS2Project parentProject = null;
        public GMS2_Resource[] resources = null;
        public string[] script_order = { "" };
        public string tutorial = "";

        public override string ToString() {
            return Name;
        }
    }

    public class GMS2_Resource {
        public string Key;
        public GMS2_Resource_Value Value;
    }
    
    public class GMS2_Resource_Value {
        public string id;
        public string resourcePath;
        public string resourceType;
    }
}
#pragma warning restore 649