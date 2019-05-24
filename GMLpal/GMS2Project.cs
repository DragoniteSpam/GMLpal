#pragma warning disable 649
namespace GMLpal {
    class GMS2Project {
        public string id;
        public string modelName;
        public string mvc;
        public string IsDnDProject;
        public object[] configs;
        public string option_ecma;

        public GMS2Project parentProject = null;

        //public GMS2_AlteredResources[] alteredResources;

        public override string ToString() {
            return "this is a gms2 project all right";
        }
    }

    public class GMS2_AlteredResources {
        public string Key;
        //public 
    }
}
#pragma warning restore 649