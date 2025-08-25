using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Tanks.Complete
{
    public class AbilityUI : MonoBehaviour
    {
        [Header("Ability Icons")]
        public Image fireIcon;
        public Image waterIcon;
        public Image windIcon;
        public Image earthIcon; 

        [Header("Visual Settings")]
        public Color availableColor = Color.white;
        public Color unavailableColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        public GameObject selectionHighlight;

        private Dictionary<PowerOrbController.ElementType, Image> iconDictionary;

        private void Awake()
        {
        
            iconDictionary = new Dictionary<PowerOrbController.ElementType, Image>
            {
                { PowerOrbController.ElementType.Fire, fireIcon },
                { PowerOrbController.ElementType.Water, waterIcon },
                { PowerOrbController.ElementType.Wind, windIcon },
                { PowerOrbController.ElementType.Earth, earthIcon } 
            };

            if (selectionHighlight != null)
            {
                selectionHighlight.SetActive(false);
            }
        }

        // --- THÊM MỚI: Hàm Start() ---
        private void Start()
        {
            // Khi game bắt đầu, gọi hàm này với một danh sách rỗng
            // để đảm bảo tất cả các icon đều được làm mờ
            HighlightAvailableAbilities(new List<PowerOrbController.ElementType>());
        }

        public void HighlightAvailableAbilities(List<PowerOrbController.ElementType> availableTypes)
        {
            // Đầu tiên, đặt tất cả các icon về trạng thái "không có sẵn"
            foreach (var icon in iconDictionary.Values)
            {
                if (icon != null) icon.color = unavailableColor;
            }

            // Sau đó, làm sáng những icon có trong danh sách
            foreach (var type in availableTypes)
            {
                if (iconDictionary.ContainsKey(type) && iconDictionary[type] != null)
                {
                    iconDictionary[type].color = availableColor;
                }
            }
        }

        public void SetSelectedAbility(PowerOrbController.ElementType selectedType)
        {
            if (selectionHighlight == null) return;

            if (selectedType == PowerOrbController.ElementType.None)
            {
                selectionHighlight.SetActive(false);
                return;
            }

            if (iconDictionary.ContainsKey(selectedType) && iconDictionary[selectedType] != null)
            {
                selectionHighlight.transform.position = iconDictionary[selectedType].transform.position;
                selectionHighlight.SetActive(true);
            }
            else
            {
                selectionHighlight.SetActive(false);
            }
        }
    }
}