using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnlockFPS : MonoBehaviour
{
    // ЭТОТ СКРИПТ СУЩЕСТВУЕТ ПОТОМУ ЧТО РАЗРАБОТЧИКИ ЮНИТИ ТУПЫЕ ДОЛБАЕБЫ, НА АНДРОИДЕ ДВИЖОК ЛОКАЕТ ФПС В 30 БЛЯТЬ, ПОЭТОМУ НАДО В РУЧНУЮ УБИРАТЬ ЭТО ДАУНСКОЕ ОГРАНИЧЕНИЕ
    void Start()
    {
        if (Application.isMobilePlatform) 
        {
            Application.targetFrameRate = 60; // ЭТО ЛИМИТ ФПС НА АНДРОИД, ЮНИТИ СОСИ ХУЯКУ (не ставьте -1, будет 30 фпс потому что юнити говно)
        }
        else 
        {
            
            Application.targetFrameRate = -1; // а это для пк, тут нет ограничений <3
        }
    }
}
