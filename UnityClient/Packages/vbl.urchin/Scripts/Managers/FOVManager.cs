using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Urchin.Behaviors;

namespace Urchin.Managers
{
    /// <summary>
    /// Field of View Manager
    /// Handles receiving data messages about FOV objects and passing them to their named object
    /// 
    /// Also handles creating/deleting FOV objects from the FOV prefab
    /// </summary>
    public class FOVManager : MonoBehaviour
    {
        #region Serialized fields
        [SerializeField] private GameObject _fovPrefabGO;
        [SerializeField] private Transform _fovParentT;
        #endregion

        #region Variables
        private Dictionary<string, FOVBehavior> _fovs;
        #endregion

        #region Unity
        void Awake()
        {
            _fovs = new();
        }
        #endregion

        #region Public

        /// <summary>
        /// Create new FOV objects
        /// </summary>
        /// <param name="names"></param>
        public void Create(List<string> names)
        {
            foreach (string name in names)
            {
                GameObject newFOV = Instantiate(_fovPrefabGO, _fovParentT);
                newFOV.name = name;

                _fovs.Add(name, newFOV.GetComponent<FOVBehavior>());
            }
        }

        /// <summary>
        /// Delete named FOV objects
        /// </summary>
        /// <param name="names"></param>
        public void Delete(List<string> names)
        {
            foreach (string name in names)
            {
                if (_fovs.ContainsKey(name))
                    Destroy(_fovs[name].gameObject);
            }
        }

        /// <summary>
        /// Change the visibility of an FOV object
        /// </summary>
        /// <param name="data"></param>
        public void SetVisibility(Dictionary<string, bool> data)
        {
            foreach (KeyValuePair<string, bool> kvp in data)
            {
                string name = kvp.Key;
                bool visible = kvp.Value;

                _fovs[name].gameObject.SetActive(visible);
            }
        }

        /// <summary>
        /// Set the corner coordinates for an FOV object
        /// </summary>
        /// <param name="data">List of Vector3 (as a List of List of floats)"</param>
        public void SetPosition(Dictionary<string, List<List<float>>> data)
        {
            foreach (KeyValuePair<string, List<List<float>>> kvp in data)
            {
                string name = kvp.Key;
                var verticesList = kvp.Value;

                List<Vector3> vertices = new();
                for (int i = 0; i < data.Count; i++)
                    vertices.Add(new Vector3(verticesList[i][0], verticesList[i][1], verticesList[i][2]));

                _fovs[name].SetPosition(vertices);
            }
        }

        //public void SetOffset(Dictionary<string, float> data)
        //{
        //    foreach (KeyValuePair<string, float> kvp in data)
        //    {
        //        string name = kvp.Key;
        //        float offsets = kvp.Value;
        //        fovRenderer.SetOffset(name, offsets);
        //    }
        //}

        //public void SetTextureDataMetaInit(List<object> data)
        //{
        //    fovRenderer.SetTextureDataMetaInit((string)data[0], (int)data[1]);
        //}

        //public void SetTextureDataMeta(List<object> data)
        //{
        //    fovRenderer.SetTextureDataMeta((string)data[0], (int)data[1], (bool)data[2]);
        //}
        //public void SetTextureData(byte[] bytes)
        //{
        //    fovRenderer.SetTextureData(bytes);
        //}

        #endregion

        #region Public helpers
        #endregion

        #region Private helpers
        #endregion
    }
}