using UnityEngine;
using System.Collections;
using UnityEngine.UI;

// Скрипт спавна ( создания новой инстанции на сцене ) техники
// Висит на кнопках, которые отображают иконки техники для создания на сцене

public class ViecleSpawnScript : MonoBehaviour
{
    // Скрипт на цене, отвечающий за логику создания новых инстанций
    public VehicleSpawnRoot spawnTarget;

    // ссылка на кнопку, по нажатии на которую произойдет попытка создания
    private UnityEngine.UI.Button button;

    // префаб на конкретный экземпляр техники.
    public GameObject objectPrefab;

    void OnEnable()
    {
        // получаем кнопку у текущего объекта и подписываемся на OnClick
        button = GetComponent<Button>();
        button.onClick.AddListener( OnButtonClick );
    }

    void OnDisable()
    {
        // отписываемся от события OnClick
        button.onClick.RemoveListener( OnButtonClick );
    }

    void OnButtonClick()
    {
        // обработчик OnClick, соответстственно вызывается при нажатии на кнопке.
        spawnTarget.Spawn( objectPrefab );
    }
}
