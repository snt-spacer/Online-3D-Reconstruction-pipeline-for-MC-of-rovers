using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Valve.VR;

public class ActionUI_Manager : MonoBehaviour
{
    // Start is called before the first frame update
    public GameObject actionMenu;
    public SteamVR_Input_Sources handType;
    public SteamVR_Action_Boolean UIAction;
    public SteamVR_Action_Vector2 SelectAction;
    public SteamVR_Action_Boolean triggerAction;
    public Transform handTransform;
    public MovementManager movementManager;

    private Button[] menuButtons;
    private int selectedButtonIndex;
    private float distanceFromHand = 0.05f;
    private Vector2 previousSelectInput = Vector2.zero;

    void Start()
    {
        menuButtons = actionMenu.GetComponentsInChildren<Button>();
        actionMenu.SetActive(false);

    }

    // Update is called once per frame
    void Update()
    {
        if (UIAction.GetStateUp(handType))
        {
            ToggleActionUI();
        }

        if (actionMenu.activeSelf)
        {
            PositionMenu();
            HandleMenuNavigation();
        }
    }


    void HandleMenuNavigation()
    {
        Vector2 selectInput = SelectAction.GetAxis(handType);

        if(selectInput.x > 0.7 && previousSelectInput.x <= 0.7f)
        {
            selectedButtonIndex = (selectedButtonIndex - 1 + menuButtons.Length) % menuButtons.Length;
            UpdateButtonHighlight();
        }
        else if(selectInput.x < -0.7f && previousSelectInput.x >= -0.7f)
        {
            selectedButtonIndex = (selectedButtonIndex + 1) % menuButtons.Length;
            UpdateButtonHighlight();
        }

        previousSelectInput = selectInput;

        if (triggerAction.GetStateDown(handType))
        {
            Button selectedButton = menuButtons[selectedButtonIndex];
            // TODO: add onClick() logic for the button
            HighlightAsSelected(selectedButton);

        }
    }

    void ToggleActionUI()
    {
        if (!actionMenu.activeSelf)
        {
            PositionMenu();
            showActionUI();
            movementManager.toggleTeleporting(false);
            movementManager.toggleSnapTurn(false);

        }
        else
        {
            hideActionUI();
            movementManager.toggleTeleporting(true);
            movementManager.toggleSnapTurn(true);
        }
    }

    void showActionUI()
    {
        actionMenu.SetActive(true);
    }

    void hideActionUI()
    {
        actionMenu.SetActive(false);
    }

    private void PositionMenu()
    {
        Vector3 offset = handTransform.up * 0.1f + handTransform.forward * distanceFromHand;
        Vector3 position = handTransform.position + offset;
        Quaternion rotation = Quaternion.LookRotation(handTransform.forward, handTransform.up);

        actionMenu.transform.position = position;
        actionMenu.transform.rotation = rotation * Quaternion.Euler(45f, 0, 0);
    }

    void UpdateButtonHighlight()
    {


        for (int i = 0; i < menuButtons.Length; i++)
        {
            var colors = menuButtons[i].colors;
            colors.normalColor = (i == selectedButtonIndex) ? Color.yellow : Color.white;
            menuButtons[i].colors = colors;
        }

    }

    void HighlightAsSelected(Button selectedButton)
    {
        var colors = selectedButton.colors;
        colors.normalColor = Color.green; // Or another “selected” color
        selectedButton.colors = colors;
    }

}
