using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class LevelSelectUI : NetworkBehaviour
{
    [SerializeField] private Button level1Button;
    [SerializeField] private Button level2Button;
    [SerializeField] private Button level3Button;
    [SerializeField] private Button level4Button;
    [SerializeField] private Button level5Button;
    [SerializeField] private Button level6Button;
    [SerializeField] private Button demoButton;

    [Header("Player Info Texts")]
    [SerializeField] private GameObject level1Text;
    [SerializeField] private GameObject level2Text;
    [SerializeField] private GameObject level3Text;
    [SerializeField] private GameObject level4Text;
    [SerializeField] private GameObject level5Text;
    [SerializeField] private GameObject level6Text;

    private void Awake()
    {
        level1Button.onClick.AddListener(() => { SelectLevel(Loader.Scene.Level1); });
        level2Button.onClick.AddListener(() => { SelectLevel(Loader.Scene.Level2); });
        level3Button.onClick.AddListener(() => { SelectLevel(Loader.Scene.Level3); });
        level4Button.onClick.AddListener(() => { SelectLevel(Loader.Scene.Level4); });
        level5Button.onClick.AddListener(() => { SelectLevel(Loader.Scene.Level5); });
        level6Button.onClick.AddListener(() => { SelectLevel(Loader.Scene.Level6); });
        demoButton.onClick.AddListener(() => { SelectLevel(Loader.Scene.Demo); });

        SetupHoverEffect(level1Button, level1Text);
        SetupHoverEffect(level2Button, level2Text);
        SetupHoverEffect(level3Button, level3Text);
        SetupHoverEffect(level4Button, level4Text);
        SetupHoverEffect(level5Button, level5Text);
        SetupHoverEffect(level6Button, level6Text);
    }

    private void Start()
    {
        if (!IsServer)
        {
            level1Button.interactable = false;
            level2Button.interactable = false;
            level3Button.interactable = false;
            level4Button.interactable = false;
            level5Button.interactable = false;
            level6Button.interactable = false;
            demoButton.interactable = false;
        }
        else
        {
            level1Button.Select();
        }
    }

    private void SelectLevel(Loader.Scene targetLevelScene)
    {
        if (IsServer)
        {
            Loader.LoadNetwork(targetLevelScene);
        }
    }

    private void SetupHoverEffect(Button button, GameObject textObj)
    {
        if (textObj == null) return;

        textObj.SetActive(false);

        EventTrigger trigger = button.gameObject.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = button.gameObject.AddComponent<EventTrigger>();
        }

        EventTrigger.Entry pointerEnter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        pointerEnter.callback.AddListener((data) => { textObj.SetActive(true); });
        trigger.triggers.Add(pointerEnter);

        EventTrigger.Entry pointerExit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        pointerExit.callback.AddListener((data) => { textObj.SetActive(false); });
        trigger.triggers.Add(pointerExit);

        EventTrigger.Entry select = new EventTrigger.Entry { eventID = EventTriggerType.Select };
        select.callback.AddListener((data) => { textObj.SetActive(true); });
        trigger.triggers.Add(select);

        EventTrigger.Entry deselect = new EventTrigger.Entry { eventID = EventTriggerType.Deselect };
        deselect.callback.AddListener((data) => { textObj.SetActive(false); });
        trigger.triggers.Add(deselect);
    }
}