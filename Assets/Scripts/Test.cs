using UnityEngine;
using UnityEngine.Events;
using PixelCrushers.DialogueSystem;
using UnityEngine.UI;


public enum QuestsType
{
    unassigned,
    active,
    success,
    failure,
    done
}


public class Test : MonoBehaviour
{
    public Button[] m_Btn;

    void Start()
    {
        m_Btn[0].onClick.AddListener(delegate () { QuestStateCut(QuestsType.unassigned); }) ;
        m_Btn[1].onClick.AddListener(delegate () { QuestStateCut(QuestsType.active); });
        m_Btn[2].onClick.AddListener(delegate () { QuestStateCut(QuestsType.success); });
        m_Btn[3].onClick.AddListener(delegate () { QuestStateCut(QuestsType.failure); });
        m_Btn[4].onClick.AddListener(delegate () { QuestStateCut(QuestsType.done); });
        DialogueLua.SetQuestField("QuestsFood", "State", PlayerPrefs.GetString("QuestsFoodState"));
    }

    public void QuestStateCut(QuestsType _type)
    {
        switch (_type)
        {
            case QuestsType.unassigned:
                DialogueLua.SetQuestField("QuestsFood", "State", "unassigned");
                PlayerPrefs.SetString("QuestsFoodState", "unassigned");
                break;
            case QuestsType.active:
                DialogueLua.SetQuestField("QuestsFood", "State", "active");
                PlayerPrefs.SetString("QuestsFoodState", "active");
                break;
            case QuestsType.success:
                DialogueLua.SetQuestField("QuestsFood", "State", "success");
                PlayerPrefs.SetString("QuestsFoodState", "success");
                break;
            case QuestsType.failure:
                DialogueLua.SetQuestField("QuestsFood", "State", "failure");
                PlayerPrefs.SetString("QuestsFoodState", "failure");
                break;
            case QuestsType.done:
                DialogueLua.SetQuestField("QuestsFood", "State", "done");
                PlayerPrefs.SetString("QuestsFoodState", "done");
                DialogueLua.SetVariable("HowTab", 2);
                Debug.Log(DialogueLua.GetVariable("HowTab").asString);
                break;
            default:
                break;
        }
        print(DialogueLua.GetQuestField("QuestsFood", "State").asString);
    }
}



