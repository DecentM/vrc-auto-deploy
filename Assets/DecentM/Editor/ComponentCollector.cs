using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DecentM.EditorTools
{
    public static class ComponentCollector<ComponentType>
    {
        public static List<ComponentType> CollectFromActiveScene()
        {
            List<ComponentType> result = new List<ComponentType>();

            Scene scene = SceneManager.GetActiveScene();

            foreach (GameObject rootObject in scene.GetRootGameObjects())
            {
                ComponentType[] components = rootObject.GetComponentsInChildren<ComponentType>();
                result.AddRange(components);
            }

            return result;
        }

        public static ComponentType CollectOneFromActiveScene()
        {
            List<ComponentType> list = CollectFromActiveScene();

            if (list.Count > 0)
                return list.ElementAt(0);

            return default(ComponentType);
        }
    }
}
