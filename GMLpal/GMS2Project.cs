using System.Collections.Generic;

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

        // [type-name, [guid, resource]]
        private Dictionary<string, Dictionary<string, GMS2_Resource>> tree = new Dictionary<string, Dictionary<string, GMS2_Resource>>();
        // [guid, resource]
        private Dictionary<string, GMS2_Resource> guids = new Dictionary<string, GMS2_Resource>();

        public override string ToString() {
            return Name;
        }

        public void Add(GMS2_Resource resource) {
            Dictionary<string, GMS2_Resource> map;

            if (!tree.ContainsKey(resource.Value.resourceType)) {
                map = new Dictionary<string, GMS2_Resource>();
                tree.Add(resource.Value.resourceType, map);
            } else {
                map = tree[resource.Value.resourceType];
            }

            map.Add(resource.Value.id, resource);

            guids.Add(resource.Value.id, resource);
        }

        public GMS2_Resource Get(string id) {
            return guids[id];
        }

        public Dictionary<string, GMS2_Resource> GetType(string type) {
            return tree[type];
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

        public bool processed = false;
    }

    public class GMS2_Folder {
        public string id;
        public string modelName;
        public string mvc;
        public string[] children;
        public string filterType;
        public string folderName;
        public bool isDefaultView;
        public string localisedFolderName;
    }
}