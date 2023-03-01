using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace E3D
{
    [RequireComponent(typeof(Button))]
    public class GUIItemListButton : GUIBase
    {
        [HideInInspector]
        public Button m_ButtonComponent;

        public Image m_ImgIcon;
        public TMP_Text m_TxtPrintName;
        public Image m_QuantityBg;
        public TMP_Text m_TxtQuantity;

        private ItemAttrib m_itemAttrib;


        public GameObject Owner { get; set; }

        public ItemAttrib ItemAttribute
        {
            get => m_itemAttrib;
            set
            {
                if (value != null)
                {
                    m_ImgIcon.sprite = value.m_Sprite;
                    m_TxtPrintName.text = value.m_PrintName;

                    if (value.m_IsInfinite)
                    {
                        m_TxtQuantity.enabled = false;
                        m_QuantityBg.enabled = false;
                    }

                    m_itemAttrib = value;
                }
            }
        }

        public AFirstAidPoint FirstAidPoint { get; set; }

        public int ItemSlotIndex { get; set; }


        protected override void Awake()
        {
            base.Awake();

            m_ButtonComponent = GetComponent<Button>();

            ItemAttribute = null;
            FirstAidPoint = null;
            ItemSlotIndex = -1;
        }

        private void Start()
        {
            m_ButtonComponent.onClick.AddListener(Button_Clicked);
        }

        private void LateUpdate()
        {
            if (FirstAidPoint != null)
            {
                if (ItemSlotIndex != -1 && ItemAttribute != null)
                {
                    int quantity = FirstAidPoint.ItemQuantities[ItemSlotIndex];

                    if (quantity > 0 || quantity == Consts.ITEM_INFINITE || ItemAttribute.m_IsInfinite)
                    {
                        m_ButtonComponent.interactable = true;
                        m_TxtQuantity.text = "x  " + quantity.ToString();
                    }
                    else
                    {
                        m_ButtonComponent.interactable = false;
                    }
                }
            }
        }

        public void ClearState()
        {
            ItemAttribute = null;
            ItemSlotIndex = -1;

            m_ImgIcon.sprite = null;
            m_TxtPrintName.text = "????????????????";
            m_TxtQuantity.text = string.Empty;
        }

        public void Button_Clicked()
        {
            if (Owner != null)
            {
                ExecuteEvents.Execute<IClickedHandler>(Owner, null,
                    (handler, e) => { handler.ItemButton_Clicked(this); });
            }
        }

        private void OnDestroy()
        {
            m_ButtonComponent.onClick.RemoveListener(Button_Clicked);
        }

        protected override void OnValidate()
        {
            base.OnValidate();

            m_ButtonComponent = gameObject.GetOrAddComponent<Button>();
        }


        public interface IClickedHandler : IEventSystemHandler
        {
            void ItemButton_Clicked(GUIItemListButton card);
        }
    }
}
