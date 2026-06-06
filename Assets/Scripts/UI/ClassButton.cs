using UnityEngine;

public class ClassButton : MonoBehaviour
{
    public CharacterClassSO classData;
    public JoinMenu joinMenu;

    public void OnClick()
    {
        if (joinMenu != null && classData != null)
        {
            joinMenu.SelectClass(classData);
        }
    }
}
