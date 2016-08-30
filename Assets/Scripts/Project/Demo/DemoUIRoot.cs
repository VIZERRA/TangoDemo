using UnityEngine;
using System.Collections;

// скрипт управления уровнем затемнения выводимым изображения с камеры
public class DemoUIRoot : MonoBehaviour
{

    // слайдер уровня затемнения
    public UnityEngine.UI.Slider slider;

    // прямая ссылка на компонент в котором изменяется значение затемнения в материале
    // *за пример взят обычный материал из TangoSDK и просто добавлен множитель ( "_ColorMultyplier" ) на цветовую состовлюящую (от 0 до 1 )
    // *этот множитель не воздействует на альфа канал

    public TangoARScreen tangoArScreen;

    private void OnEnable()
    {
        // при активации скрипта поставить значение слайдера, взятое из материала
        slider.value = tangoArScreen.m_screenMaterial.GetFloat( "_ColorMultyplier" );
        // подписаться на событие изменения слайдера
        slider.onValueChanged.AddListener( OnSliderValueChanged );
    }

    private void OnDisable()
    {
        // отписаться от события изменения слайдера
        slider.onValueChanged.AddListener( OnSliderValueChanged );
    }

    private void OnSliderValueChanged( float value )
    {
        // при изменении слайдера изменить уровень яркости изоброжения, через изменения множителя цветовых компонентов ( "_ColorMultyplier" )
        tangoArScreen.m_screenMaterial.SetFloat( "_ColorMultyplier", value );
    }

    void Update()
    {
        // проверяем число пальцев, если меньше 5 то это не наш случай, выходим
        if ( Input.touchCount < 5 )
            return;

        // если событие от последнего пальца не соответствует состоянию TouchPhase.Began, то это не наш случай, выходим
        if ( Input.GetTouch( 4 ).phase != TouchPhase.Began )
            return;

        // просто меняем активность слайдера, по умолчанию он скрыт, так как это инструмент настнойки, который запросили
        // для настройки под определенную освещенность перед показом
        slider.gameObject.SetActive( !slider.gameObject.activeSelf );
    }
}
