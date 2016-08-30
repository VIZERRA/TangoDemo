using System;
using Tango;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using UnityEngine.EventSystems;

//using Vizerra.UI;

// Скрипт логики расстановки расстановки техники в сцене
// Здесь реализована основная логика

public class VehicleSpawnRoot : MonoBehaviour, ITangoLifecycle, ITangoDepth
{
    // лимит инстанций, для предотвращения падения производительноси. Может быть настроен на сцене
    public int instancesLimit = 10;

    // дополнительный множитель масштаба моделей, может быть изменен на сцене
    public float modelsScale = 1.0f;

    // вызывается перед расстановкой
    // если расставленных элементов меньше чем лимит, то выходим
    // в противном случае удаляем самый старый
    void CheckForLimits()
    {
        if ( transform.childCount < instancesLimit )
        {
            return;
        }

        GameObject.Destroy( transform.GetChild( 0 ).gameObject );
    }

    // Временная марка, ограничение на некоторые действия пользователя
    float timeFromSpawn = 0;

    // удаление всех расставленных объектов
    // вызывается прямой приязкой к кнопке 'Clear' в верхнем правом углу
    public void Clear()
    {
        int count = transform.childCount;
        while ( count > 0 )
        {
            GameObject.Destroy( transform.GetChild( --count ).gameObject );
        }
    }

    // проверка на пересечение с существующей моделью
    // указывается специфический слой для трейса ( "traceLayerMask" )
    bool CheckForModelIntersection( Vector2 screenPos )
    {
        var cam = Camera.main;
        if ( cam != null )
        {
            var ray = new Ray( cam.transform.position, tracePlainCenter - cam.transform.position );
            RaycastHit hit;
            if ( Physics.Raycast( ray, out hit, Mathf.Infinity, traceLayerMask ) )
            {
                return true;
            }
        }

        return false;
    }

    // попытка создать объект, из префаба в текущем положении маркера
    public void Spawn( GameObject prefab )
    {
        // проверка, не заблокировано ли действие расстановки?
        if ( !IsActionAllowed() )
            return;

        // не попадаем ли мы в существующую (уже расставленную) модель ?
        if ( CheckForModelIntersection() )
            return;

        // проверяем результаты асиннхронного трейса внутри PointsCloud
        switch ( traceResults )
        {
            case ETraceResults.Invalid: // результаты не доступны
            case ETraceResults.NotFound: // результаты доступны, но пересечение с плоскостью не найдено. Расстановка запрещена
            case ETraceResults.NotGround: // перузьтаты доступны, но параметры не соответствуют параметрам расстановки на горизонтальной поверхности
                return;
        }

        // проверка лимита расставленных объектов и удаление самых старых, если необходимо
        CheckForLimits();

        // Инстанцируем новый объект, даем ему имя для отладки и читаемости сцены
        var obj = ( GameObject ) Instantiate( prefab );
        obj.transform.SetParent( transform, true );
        obj.transform.position = tracePlainCenter;
        obj.transform.localScale = prefab.transform.localScale * modelsScale;
        obj.SetActive( true );
        obj.name = prefab.name + "_" + instanceNumber.ToString();
        ++instanceNumber;

        // сообщаем о том что действие обработано
        MarkAction();
    }

    // счетчик созданных инстанций, используется только для отладки и наименования объектов в сцене.
    // не сбрасывается и не уменьшается
    int instanceNumber;

    // создаем верменную метку, что действие обработано. Блокирует повторыые действия в течении короткого промежутка времени
    private void MarkAction()
    {
        timeFromSpawn = Time.realtimeSinceStartup;
    }

    // ссылка на компонент ( "TangoApplication" ) - компонент TangoSDK
    private TangoApplication m_tangoApplication;

    // прямая ссылка на компонент ( "TangoApplication" ) - компонент TangoSDK
    public TangoPointCloud m_pointCloud;

    // метка (маркер) расстановки
    public GameObject placingMark;

    // маска слоев для трейса локальных объектов при поиске уже расставленных моделей в сцене дополненной реальности
    public LayerMask traceLayerMask;

    // при запуске получаем ссылку на (TangoApplication), пологаю что он допустим только один
    public void Start()
    {
        m_tangoApplication = FindObjectOfType<TangoApplication>();
        // привязываемся к функционалу танго
        m_tangoApplication.Register( this );
    }

    /// <summary>
    /// Unity destroy function.
    /// </summary>
    public void OnDestroy()
    {
        // отвязываемся от функционала танго
        m_tangoApplication.Unregister( this );
    }

    // взято из примера, используется в асинхронном распознании пересечения
    private bool m_DepthReady = false;

    // каллбек танго, устанавливает m_DepthReady и позволяет дальнийшие асинхронные действия для поиска пересечения
    public void OnTangoDepthAvailable( TangoUnityDepth tangoDepth )
    {
        m_DepthReady = true;
    }

    // признак, коворящий о том что необходимо сотановить асинхронное исполнение. Возможно этот код лишний
    private bool m_ShouldTerminate = false;

    // Перечислитель, показывающий состояние
    enum ETraceResults
    {
        Invalid, // результаты трейса недоступны
        NotFound, // результаты трейса доступны но пересечения с плоскостью не найдено
        NotGround, // плоскость не выровнена по горизонтали. * я не стал проверять знак, так что объекты можно расставить и на потолку :-D (так как тестирование должно было проходить на открытом воздухе :) )
        ExistingObjectCrossing, // обнаружено пересечение с расставленным объектом дополненной реальности
        Valid, // подходящее положение для расстановки
    }

    // координаты плейна из результатов трейса в PointsCloud
    private Vector3 tracePlainCenter;
    
    // состояние трейса
    private ETraceResults traceResults = ETraceResults.Invalid;

    // ф-ция хелпер для парной установки результатов трейса
    void SetTraceResults( Vector3 placePos, ETraceResults res )
    {
        this.tracePlainCenter = placePos;
        traceResults = res;
    }

    // установка экранных координат для трейса, сбрасывает состояние трейса в недоступное
    void TraceScreenPos( Vector2 pos )
    {
        traceScreenPos = pos;
        traceResults = ETraceResults.Invalid;
    }

    // сброс экранных координат трейса в дефалтные (центр экрана)
    void ResetTraceScreenPos()
    {
        TraceScreenPos( new Vector2( Screen.width / 2, Screen.height / 2 ) );
    }

    // как только компонент активируется, мы запускаем асинхронную работу по трейсу
    void OnEnable()
    {
        ResetTraceScreenPos();
        StartCoroutine( LazyUpdate() );
    }

    // если компонент дективировался, запрашиваем выход созданной корутины.
    // возможно это не совсем корректный код
    void OnDisable()
    {
        m_ShouldTerminate = true;
    }

    // ф-ция для асинхронного поиска параметров расстановки
    private IEnumerator LazyUpdate()
    {
        while ( !m_ShouldTerminate ) // работаем пока не запросили останов
        {
            while ( !m_ShouldTerminate ) // аналогично
            {
                if ( !tangoConnected ) // если функционал танго не подключен, уходим в цикл верхнего уровня
                {
                    break;
                }

                if ( !m_DepthReady ) // если буфер глубены недоступен, выходим в цикл верхнего уровня
                {
                    break;
                }

                // Find the plane. // этот код большей частью взят из премера демки Ar
                Camera cam = Camera.main;
                if ( cam == null )
                {
                    // что то посшло не так, главная камера null :) Устанавливаем результаты трейса как ETraceResults.NotFound
                    SetTraceResults( Vector3.zero, ETraceResults.NotFound );
                    break;
                }

                Vector3 planeCenter;
                Plane plane;
                if ( !m_pointCloud.FindPlane( cam, traceScreenPos, out planeCenter, out plane ) )
                {
                    // не смогли найдти пересечение, помечаем результаты трейса как ETraceResults.NotFound
                    SetTraceResults( Vector3.zero, ETraceResults.NotFound );
                    break;
                }

                // Ensure the location is always facing the camera.  This is like a LookRotation, but for the Y axis.
                Vector3 up = plane.normal;
                Vector3 forward;
                if ( Vector3.Angle( plane.normal, cam.transform.forward ) < 175 )
                {
                    Vector3 right = Vector3.Cross( up, cam.transform.forward ).normalized;
                    forward = Vector3.Cross( right, up ).normalized;
                }
                else
                {
                    // Normal is nearly parallel to camera look direction, the cross product would have too much
                    // floating point error in it.
                    forward = Vector3.Cross( up, cam.transform.right );
                }

                {
                    var cross = Vector3.Cross( plane.normal, Vector3.up );
                    if ( Mathf.Abs( cross.x ) >= 0.1f && Mathf.Abs( cross.z ) >= 0.1f )
                    {
                        // проверяем на то что найденный плейн соноправлен горизонтали, для того что бы мы могли расставлять объекты только в горизонтальной плоскости
                        // здесь не учитывается знак, поэтому возможно расставлять не только на полу, столу и прочем но и на потолку :)
                        // помечаем результаты трейса, как ETraceResults.NotGround
                        SetTraceResults( planeCenter, ETraceResults.NotGround );
                        break;
                    }
                }

                // помечаем результаты трейса как ETraceResults.Valid
                SetTraceResults( planeCenter, ETraceResults.Valid );
                // переходим в цикл верхнего уровня
                break;
            }

            // ограничение по частоте обновления
            yield return new WaitForSeconds( 1 / 30.0f );
        }
    }

    // dummy interface implementation
    public void OnTangoPermissions( bool permissionsGranted )
    {
    }

    // признак того что сервис танго подключен
    bool tangoConnected = false;

    public void OnTangoServiceConnected()
    {
        // каллбек уведомляющий подключение сервиса танго
        UnityEngine.Debug.Log( "LazyUpdate.OnTangoServiceConnected" );
        tangoConnected = true;
        m_DepthReady = false;
        m_ShouldTerminate = false;
        m_tangoApplication.SetDepthCameraRate( TangoEnums.TangoDepthCameraRate.MAXIMUM );
    }

    public void OnTangoServiceDisconnected()
    {
        // каллбек уведомляющий отключение сервиса танго
        tangoConnected = false;
        UnityEngine.Debug.Log( "LazyUpdate.OnTangoServiceDisconnected" );
        m_tangoApplication.SetDepthCameraRate( TangoEnums.TangoDepthCameraRate.DISABLED );
        m_ShouldTerminate = true;
    }


    // прямая ссылка на объект, вкотором есть текст для отображения текстовой информации об объекте
    public GameObject selectionInfo;

    // выбранный объект расстановки, используется при таскании
    private GameObject selected;
    private GameObject Selected
    {
        get { return selected; }
        set
        {
            selected = value;

            // при изменении выделенного объекта, обновляем текст подсказки
            if ( selectionInfo != null )
            {
                selectionInfo.SetActive( selected != null );
                var textComponent = selectionInfo.GetComponentInChildren<UnityEngine.UI.Text>();
                if ( textComponent != null && selected != null )
                {
                    textComponent.text = selected.name;
                }
            }
        }
    }
    
    // подсвеченный объект, внутренняя логика, (подсветка не отображается)
    private GameObject hightLighted;
    private GameObject HightLighted
    {
        get { return hightLighted; }
        set
        {
            hightLighted = value;
        }
    }

    // экранные координаты для трейса
    private Vector2 traceScreenPos;

    // ф-ция возвращает элемент UI под курсором
    // по хорошему все необьходимое для GetUIObjectUnder нужно было вынести в отдельный класс
    private GameObject GetUIObjectUnder( Vector2 pos )
    {
        if ( EventSystem.current == null )
            return null;

        if ( eventData == null )
        {
            eventData = new PointerEventData( EventSystem.current );
        } else
        {
            eventData.Reset();
        }
        eventData.position = pos;
        EventSystem.current.RaycastAll( eventData, raycastResults );

        var raycast = FindFirstRaycast( raycastResults );
        eventData.pointerCurrentRaycast = raycast;
        raycastResults.Clear();
        return raycast.gameObject;
    }

    // используется для GetUIObjectUnder
    protected static RaycastResult FindFirstRaycast( List<RaycastResult> candidates )
    {
        for ( var i = 0; i < candidates.Count; ++i )
        {
            if ( candidates[ i ].gameObject == null )
                continue;

            return candidates[ i ];
        }
        return new RaycastResult();
    }

    // используется для GetUIObjectUnder
    PointerEventData eventData;
    // используется для GetUIObjectUnder
    List<RaycastResult> raycastResults = new List<RaycastResult>();

    // проверка на возможность выполения действия.
    // Условием является, что прошло не менее 0,3с с прошлого действия
    // workaround от некоторых проблем, связанных с Input.Touch и UI
    private bool IsActionAllowed()
    {
        if ( timeFromSpawn > 0 )
        {
            if ( ( Time.realtimeSinceStartup - timeFromSpawn ) < 0.3f )
                return false;
        }

        return true;
    }

    // вызывается из Update
    // отвечает за таскание выделенного объекта
    private void UpdateTouchesMove()
    {
        if ( Input.touchCount == 0 )
        {
            // если нет пальцев на экране - сброс координат трейса в стандартное положение, и выход
            if ( Selected != null )
            {
                Selected = null;
                ResetTraceScreenPos();
            }
            return;
        }


        //if ( !IsActionAllowed() )
        //    return;

        var touch = Input.GetTouch( 0 );
        // получаем первый тач


        switch ( touch.phase )
        {
                // при прикосновении
            case TouchPhase.Began:
                {
                    // если элемент интерфейса по этим ккрдинатам, то выходим
                    if ( GetUIObjectUnder( touch.position ) != null )
                    {
                        return;
                    }

                    // если по какой либо причине главная камера ноль, то выходим
                    Camera cam = Camera.main;
                    if ( cam == null )
                        return;

                    // ищем пересечение с расставленными объектами в сцене
                    // так как объекты могут иметь произвольно сложную конструкцию, то нам нужно найдти локальный Root для найденного объекта
                    RaycastHit hitInfo;
                    TraceScreenPos( touch.position );
                    GameObject obj = null;
                    if ( Physics.Raycast( cam.ScreenPointToRay( touch.position ), out hitInfo ) )
                    {
                        // Found a marker, select it (so long as it isn't disappearing)!
                        obj = hitInfo.collider != null ? hitInfo.collider.gameObject : null;
                        if ( obj != null )
                        {
                            for ( ; ; )
                            {
                                if ( obj.transform.parent == transform || obj.transform.parent == null )
                                    break;
                                obj = obj.transform.parent.gameObject;
                            }
                        }
                    }

                    // устанавливаем выделенный объект
                    Selected = obj;
                    // помечаем действие выполненным
                    MarkAction();
                    break;
                }
                // палец отпущен
            case TouchPhase.Ended:
                // сбрасываем результаты трейса в центр экрана
                ResetTraceScreenPos();
                // сбрасываем выделенный объект
                Selected = null;
                // помечаем действие как выполненное
                MarkAction();
                break;
            default:
                // обновляем координаты трейса в соответствии с координатами тача (используется при таскании объекта)
                traceScreenPos = touch.position;
                if ( traceResults == ETraceResults.Invalid )
                    break;
                // Если координаты трейса допустимые (найдено пересечение с плоскостью) то выделенный объект перемещается по этим координатам
                // здесть не запрещено перемещение объекта в другой объект или перетаскивание его например на стену
                if ( Selected != null )
                {
                    Selected.transform.position = tracePlainCenter;
                }
                break;
        }

        // если нет выделенного объекта - сбрасываем экранные координаты трейса в стандартные
        if ( Selected == null )
        {
            ResetTraceScreenPos();
        }
    }

    // ищет пересечение с расставленным объектом
    private bool CheckForModelIntersection()
    {
        var cam = Camera.main;
        if ( cam != null && traceResults == ETraceResults.Valid )
        {
            var ray = new Ray( cam.transform.position, tracePlainCenter - cam.transform.position );
            RaycastHit hit;
            if ( Physics.Raycast( ray, out hit, Mathf.Infinity, traceLayerMask ) )
            {
                traceResults = ETraceResults.ExistingObjectCrossing;
                HightLighted = hit.transform.gameObject;
                return true;
            }
            else
            {
                HightLighted = null;
            }
        }
        else
        {
            HightLighted = null;
        }

        return false;
    }

    // ф-ция Update
    public void Update()
    {
        // проверяем пересечения с существующими объектами
        CheckForModelIntersection();

        // вызываем ф-цию, для обновления перетаскивания объекта
        UpdateTouchesMove();

        // обновляем метку расстановки (координаты и цвет, в зависимости от состояния трефса)
        if ( placingMark != null )
        {
            bool gotCoordinates = traceResults != ETraceResults.Invalid;
            placingMark.SetActive( gotCoordinates && Selected == null );
            if ( gotCoordinates )
            {
                placingMark.transform.position = tracePlainCenter;
            }

            if ( traceResults == ETraceResults.Valid )
            {
                placingMark.GetComponent<MeshRenderer>().material.color = new Color( 50.0f / 255, 1, 50.0f / 255, 1 );
            }
            else
            {
                placingMark.GetComponent<MeshRenderer>().material.color = new Color( 1, 0, 0, 1 );
            }
        }

        // ну и соответственно обработчик выхода
        if ( Input.GetKey( KeyCode.Escape ) )
        {
            Application.Quit();
        }
    }
}
