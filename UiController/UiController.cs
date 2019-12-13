using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TSR.UI
{
    public abstract class UiController<TUiElement, TContext> : MonoBehaviour where TUiElement : Enum where TContext : Enum
    {
        protected Dictionary<TUiElement, GameObject> uiElements = new Dictionary<TUiElement, GameObject>();

        public TContext CurrentContext
        {
            get { return currentContext; }
            protected set { currentContext = value; }
        }
        protected TContext currentContext;

        public abstract void PreviousContext();

        public abstract void SwitchContext(TContext context);

        public void SwitchConext(int context)
        {
            SwitchContext((TContext)context);
        }
        
        protected void FindAllElements()
        {
            var gameObjects = transform.GetComponentsInChildren<Transform>(true);

            foreach (var element in (TUiElement[])Enum.GetValues(typeof(TUiElement)))
            {
                var tmp = gameObjects.FirstOrDefault(go => go.name == element.ToString());

                if (tmp == null)
                {
                    Debug.LogError($"Can't find object with name: {element.ToString()}");
                    continue;
                }

                uiElements[element] = tmp.gameObject;
            }
        }

        protected void SetElementsState(params TUiElement[] elementsToActivate)
        {
            foreach (KeyValuePair<TUiElement, GameObject> element in uiElements)
            {
                element.Value.SetActive(elementsToActivate.Any(e => e.CompareTo(element.Key) == 0));
            }
        }
    }
}
