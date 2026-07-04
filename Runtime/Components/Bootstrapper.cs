using UnityEngine;
using UnityEngine.UIElements;
using Armoury.UI.Injectors;
using Sirenix.OdinInspector;

namespace Armoury.UI
{
    [AddComponentMenu("UniNg/Bootstrapper")]
    [RequireComponent(typeof(UIDocument))]
    public abstract class Bootstrapper<TComp> : MonoBehaviour where TComp : Component, new()
    {
        [SerializeField] private UIDocument _document;
        [SerializeField, InlineEditor] private EnvironmentInjectorDefinition _injectorDefinition;

        [SerializeField] private CursorState _cursorState;

        private Injector _injector;

        private void Awake()
        {
            SetCursorState();
            CreateInjector();
            Bootstrap<TComp, VisualElement>();
        }

        private void SetCursorState()
        {
            if (_cursorState == CursorState.Inherit)
            {
                return;
            }

            UnityEngine.Cursor.visible = _cursorState switch
            {
                CursorState.VisibleDefault => true,
                CursorState.VisibleLocked => true,
                CursorState.VisibleConfined => true,
                _ => false
            };
                
            UnityEngine.Cursor.lockState = _cursorState switch
            {
                CursorState.VisibleDefault => CursorLockMode.None,
                CursorState.HiddenDefault => CursorLockMode.None,
                CursorState.VisibleLocked => CursorLockMode.Locked,
                CursorState.HiddenLocked => CursorLockMode.Locked,
                CursorState.VisibleConfined => CursorLockMode.Confined,
                CursorState.HiddenConfined => CursorLockMode.Confined,
                _ => CursorLockMode.None
            };
        }

        private void CreateInjector()
        {
            _injector = new Injector();
            
            _injector.Inject(
                new ElementRef.Provider(new ElementRef(_document.rootVisualElement, _injector))
            );

            _injectorDefinition.InjectInto(_injector);
        }

        private void Bootstrap<UComp, UElement>()
            where UComp : Component, new() where UElement : VisualElement
        {
            ViewContainerRef.CreateInPlace<UComp, UElement>((UElement) _document.rootVisualElement, _injector);
        }

        private void OnValidate()
        {
#if UNITY_EDITOR
            if (!_document && !Application.isPlaying && UnityEditor.Selection.activeGameObject == gameObject)
            {
                _document = GetComponent<UIDocument>();
            }
#endif
        }

        private enum CursorState
        {
            Inherit = 0,
            VisibleDefault, VisibleLocked, VisibleConfined,
            HiddenDefault, HiddenLocked, HiddenConfined
        }
    }
}