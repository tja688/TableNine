
## API Doc / API 文档

### API Index

- [00.FluentAPI.Unity](#api-group-00-fluentapi-unity)
  - [UnityEngine.Object](#unityengine-object-unityengineobjectextension)
  - [UnityEngine.GameObject](#unityengine-gameobject-unityenginegameobjectextension)
  - [UnityEngine.Transform](#unityengine-transform-unityenginetransformextension)
  - [UnityEngine.MonoBehaviour](#unityengine-monobehaviour-unityenginemonobehaviourextension)
  - [UnityEngine.Camera](#unityengine-camera-unityenginecameraextension)
  - [UnityEngine.Color](#unityengine-color-unityenginecolorextension)
  - [UnityEngine.Graphic](#unityengine-graphic-unityengineuigraphicextension)
  - [UnityEngine.Random](#unityengine-random-randomutility)
  - [UnityEngine.Others](#unityengine-others-unityengineothersextension)
  - [UnityEngine.Vector2/3](#unityengine-vector2-3-unityenginevectorextension)
  - [UnityEngine.RectTransform](#unityengine-recttransform-unityenginerecttransformextension)
- [01.FluentAPI.CSharp](#api-group-01-fluentapi-csharp)
  - [System.Object](#system-object-systemobjectextension)
  - [System.String](#system-string-systemstringextension)
  - [System.IO](#system-io-systemioextension)
  - [System.Collections](#system-collections-collectionsextension)
  - [System.Reflection](#system-reflection-systemreflectionextension)
- [02.LogKit](#api-group-02-logkit)
  - [LogKit](#logkit-logkit)
- [03.SingletonKit](#api-group-03-singletonkit)
  - [MonoSingleton<T>](#monosingletont-monosingletont)
  - [Singleton<T>](#singletont-singletont)
  - [MonoSingletonProperty<T>](#monosingletonpropertyt-monosingletonpropertyt)
  - [SingletonProperty<T>](#singletonpropertyt-singletonpropertyt)
  - [MonoSingletonPath](#monosingletonpath-monosingletonpathattribute)
  - [PersistentMonoSingleton<T>](#persistentmonosingletont-persistentmonosingletont)
  - [ReplaceableMonoSingleton<T>](#replaceablemonosingletont-replaceablemonosingletont)
- [04.ActionKit](#api-group-04-actionkit)
  - [ActionKit](#actionkit-actionkit)
- [05.TableKit](#api-group-05-tablekit)
  - [Table<T>](#tablet-tablet)
- [06.PoolKit](#api-group-06-poolkit)
  - [SimpleObjectPool<T>](#simpleobjectpoolt-simpleobjectpoolt)
  - [ListPool<T>](#listpoolt-listpoolt)
  - [DictionaryPool<T,K>](#dictionarypooltk-dictionarypooltk)
  - [SafeObjectPool<T>](#safeobjectpoolt-safeobjectpoolt)
- [07.ResKit](#api-group-07-reskit)
  - [ResKit](#reskit-reskit)
  - [ResLoader Object](#resloader-object-resloader-object)
  - [ResLoader API](#resloader-api-resloader-api)
- [08.UIKit](#api-group-08-uikit)
  - [UIKit](#uikit-uikit)
- [09.AudioKit](#api-group-09-audiokit)
  - [AudioKit](#audiokit-audiokit)
- [10.FSM](#api-group-10-fsm)
  - [FSM](#fsm-fsm)
- [11.GridKit](#api-group-11-gridkit)
  - [EasyGrid](#easygrid-easygrid)
  - [DynaGrid](#dynagrid-dynagrid)

---

### API Group: 00.FluentAPI.Unity

#### UnityEngine.Object (UnityEngineObjectExtension)

- Type: `QFramework.UnityEngineObjectExtension`
- Namespace: `QFramework`

**Description / 描述**

- 描述: 针对 UnityEngine.Object 提供的链式扩展
- Description: The chain extension provided by UnityEngine.Object

**Example / 示例**

```csharp
var gameObject = new GameObject();
//
gameObject.Instantiate()
        .Name("ExtensionExample")
        .DestroySelf();
//
gameObject.Instantiate()
        .DestroySelfGracefully();
//
gameObject.Instantiate()
        .DestroySelfAfterDelay(1.0f);
//
gameObject.Instantiate()
        .DestroySelfAfterDelayGracefully(1.0f);
//
gameObject
        .Self(selfObj => Debug.Log(selfObj.name))
        .Name("TestObj")
        .Self(selfObj => Debug.Log(selfObj.name))
        .Name("ExtensionExample")
        .DontDestroyOnLoad();
```

**Methods / 方法**

##### void DestroySelf<T>(T selfObj)

- 描述: Object.Destroy(Object) 简单链式封装
- Description: Object.Destroy(Object) extension

```csharp
new GameObject().DestroySelf()
```

##### T DestroySelfAfterDelay<T>(T selfObj, float afterDelay)

- 描述: Object.Destroy(Object,float) 简单链式封装
- Description: Object.Destroy(Object,float) extension

```csharp
new GameObject().DestroySelfAfterDelay(5);
```

##### T DestroySelfAfterDelayGracefully<T>(T selfObj, float delay)

- 描述: Object.Destroy(Object,float) 简单链式封装
- Description: Object.Destroy(Object,float) extension

```csharp
GameObject gameObj = null;
gameObj.DestroySelfAfterDelayGracefully(5);
// not throw exception
// 不会报异常
```

##### T DestroySelfGracefully<T>(T selfObj)

- 描述: Object.Destroy(Object) 简单链式封装
- Description: Object.Destroy(Object) extension

```csharp
GameObject gameObj = null;
gameObj.DestroySelfGracefully();
// not throw null exception
// 这样写不会报异常(但是不好调试)
```

##### T DontDestroyOnLoad<T>(T selfObj)

- 描述: Object.DontDestroyOnLoad 简单链式封装
- Description: Object.DontDestroyOnLoad extension

```csharp
new GameObject().DontDestroyOnLoad();
```

##### T Instantiate<T>(T selfObj)

- 描述: Object.Instantiate(Object) 的简单链式封装
- Description: Object.Instantiate(Object) extension

```csharp
prefab.Instantiate();
```

##### T Instantiate<T>(T selfObj, Vector3 position, Quaternion rotation)

- 描述: Object.Instantiate(Object,Vector3,Quaternion) 的简单链式封装
- Description: Object.Instantiate(Object,Vector3,Quaternion) extension

```csharp
prefab.Instantiate(Vector3.zero,Quaternion.identity);
```

##### T Instantiate<T>(T selfObj, Vector3 position, Quaternion rotation, Transform parent)

- 描述: Object.Instantiate(Object,Vector3,Quaternion,Transform parent) 的简单链式封装
- Description: Object.Instantiate(Object,Vector3,Quaternion,Transform parent) extension

```csharp
prefab.Instantiate(Vector3.zero,Quaternion.identity,transformRoot);
```

##### T InstantiateWithParent<T>(T selfObj, Transform parent)

- 描述: Object.Instantiate(Transform parent) 的简单链式封装
- Description: Object.Instantiate(Transform parent) extension

```csharp
prefab.Instantiate(transformRoot);
```

##### T InstantiateWithParent<T>(T selfObj, Transform parent, bool worldPositionStays)

- 描述: Object.Instantiate(Transform parent,bool worldPositionStays) 的简单链式封装
- Description: Object.Instantiate(Transform parent,bool worldPositionStays) extension

```csharp
prefab.Instantiate(transformRoot,true);
```

##### T Name<T>(T selfObj, string name)

- 描述: 设置名字
- Description: set Object's name

```csharp
scriptableObject.Name("LevelData");
Debug.Log(scriptableObject.name);
// LevelData
```



---

#### UnityEngine.GameObject (UnityEngineGameObjectExtension)

- Type: `QFramework.UnityEngineGameObjectExtension`
- Namespace: `QFramework`

**Description / 描述**

- 描述: 针对 UnityEngine.GameObject 提供的链式扩展
- Description: The chain extension provided by UnityEngine.Object.

**Example / 示例**

```csharp
var gameObject = new GameObject();
var transform = gameObject.transform;
var selfScript = gameObject.AddComponent<MonoBehaviour>();
var boxCollider = gameObject.AddComponent<BoxCollider>();
//
gameObject.Show(); // gameObject.SetActive(true)
selfScript.Show(); // this.gameObject.SetActive(true)
boxCollider.Show(); // boxCollider.gameObject.SetActive(true)
gameObject.transform.Show(); // transform.gameObject.SetActive(true)
//
gameObject.Hide(); // gameObject.SetActive(false)
selfScript.Hide(); // this.gameObject.SetActive(false)
boxCollider.Hide(); // boxCollider.gameObject.SetActive(false)
transform.Hide(); // transform.gameObject.SetActive(false)
//
selfScript.DestroyGameObj();
boxCollider.DestroyGameObj();
]transform.DestroyGameObj();
//
selfScript.DestroyGameObjGracefully();
boxCollider.DestroyGameObjGracefully();
transform.DestroyGameObjGracefully();
//
selfScript.DestroyGameObjAfterDelay(1.0f);
boxCollider.DestroyGameObjAfterDelay(1.0f);
transform.DestroyGameObjAfterDelay(1.0f);
//
selfScript.DestroyGameObjAfterDelayGracefully(1.0f);
boxCollider.DestroyGameObjAfterDelayGracefully(1.0f);
transform.DestroyGameObjAfterDelayGracefully(1.0f);
//
gameObject.Layer(0);
selfScript.Layer(0);
boxCollider.Layer(0);
transform.Layer(0);
//
gameObject.Layer("Default");
selfScript.Layer("Default");
boxCollider.Layer("Default");
transform.Layer("Default");
```

**Methods / 方法**

##### void DestroyGameObj<T>(T selfBehaviour)

- 描述: Destroy(myScript.gameObject)
- Description: Destroy(myScript.gameObject)

```csharp
myScript.DestroyGameObj();
```

##### T DestroyGameObjAfterDelay<T>(T selfBehaviour, float delay)

- 描述: Object.Destroy(myScript.gameObject,delaySeconds)
- Description: Object.Destroy(myScript.gameObject,delaySeconds)

```csharp
myScript.DestroyGameObjAfterDelay(5);
```

##### T DestroyGameObjAfterDelayGracefully<T>(T selfBehaviour, float delay)

- 描述: if (myScript && myScript.gameObject) Object.Destroy(myScript.gameObject,delaySeconds)
- Description: if (myScript && myScript.gameObject) Object.Destroy(myScript.gameObject,delaySeconds)

```csharp
myScript.DestroyGameObjAfterDelayGracefully(5);
```

##### void DestroyGameObjGracefully<T>(T selfBehaviour)

- 描述: if (myScript) Destroy(myScript.gameObject)
- Description: if (myScript) Destroy(myScript.gameObject)

```csharp
myScript.DestroyGameObjGracefully();
```

##### T GetOrAddComponent<T>(GameObject self)

- 描述: 获取组件，没有则添加再返回
- Description: Get component, add and return if not

```csharp
gameObj.GetOrAddComponent<SpriteRenderer>();
```

##### T GetOrAddComponent<T>(Component component)

- 描述: 获取组件，没有则添加再返回
- Description: Get component, add and return if not

```csharp
component.GetOrAddComponent<SpriteRenderer>();
```

##### Component GetOrAddComponent(GameObject self, Type type)

- 描述: 获取组件，没有则添加再返回
- Description: Get component, add and return if not

```csharp
gameObj.GetOrAddComponent(typeof(SpriteRenderer));
```

##### GameObject Hide(GameObject selfObj)

- 描述: gameObject.SetActive(false)
- Description: gameObject.SetActive(false)

```csharp
gameObject.Hide();
```

##### T Hide<T>(T selfComponent)

- 描述: myScript.gameObject.SetActive(false)
- Description: myScript.gameObject.SetActive(false)

```csharp
GetComponent<MyScript>().Hide();
```

##### bool IsInLayerMask(GameObject selfObj, LayerMask layerMask)

- 描述: layerMask 中的层级是否包含 gameObj 所在的层级
- Description: Whether the layer in layerMask contains the same layer as gameObj

```csharp
gameObj.IsInLayerMask(layerMask);
```

##### bool IsInLayerMask<T>(T selfComponent, LayerMask layerMask)

- 描述: layerMask 中的层级是否包含 component.gameObject 所在的层级
- Description: Whether the layer in layerMask contains the same layer as component.gameObject

```csharp
spriteRenderer.IsInLayerMask(layerMask);
```

##### GameObject Layer(GameObject selfObj, int layer)

- 描述: gameObject.layer = layer
- Description: gameObject.layer = layer

```csharp
new GameObject().Layer(0);
```

##### T Layer<T>(T selfComponent, int layer)

- 描述: component.gameObject.layer = layer
- Description: component.gameObject.layer = layer

```csharp
rigidbody2D.Layer(0);
```

##### GameObject Layer(GameObject selfObj, string layerName)

- 描述: gameObj.layer = LayerMask.NameToLayer(layerName)
- Description: gameObj.layer = LayerMask.NameToLayer(layerName)

```csharp
new GameObject().Layer("Default");
```

##### T Layer<T>(T selfComponent, string layerName)

- 描述: component.gameObject.layer = LayerMask.NameToLayer(layerName)
- Description: component.gameObject.layer = LayerMask.NameToLayer(layerName)

```csharp
spriteRenderer.Layer("Default");
```

##### GameObject Show(GameObject selfObj)

- 描述: gameObject.SetActive(true)
- Description: gameObject.SetActive(true)

```csharp
new GameObject().Show();
```

##### T Show<T>(T selfComponent)

- 描述: script.gameObject.SetActive(true)
- Description: script.gameObject.SetActive(true)

```csharp
GetComponent<MyScript>().Show();
```



---

#### UnityEngine.Transform (UnityEngineTransformExtension)

- Type: `QFramework.UnityEngineTransformExtension`
- Namespace: `QFramework`

**Description / 描述**

- 描述: 针对 UnityEngine.GameObject 提供的链式扩展
- Description: The chain extension provided by UnityEngine.Object.

**Example / 示例**

```csharp
var selfScript = new GameObject().AddComponent<MonoBehaviour>();
var transform = selfScript.transform;

transform
    .Parent(null)
    .LocalIdentity()
    .LocalPositionIdentity()
    .LocalRotationIdentity()
    .LocalScaleIdentity()
    .LocalPosition(Vector3.zero)
    .LocalPosition(0, 0, 0)
    .LocalPosition(0, 0)
    .LocalPositionX(0)
    .LocalPositionY(0)
    .LocalPositionZ(0)
    .LocalRotation(Quaternion.identity)
    .LocalScale(Vector3.one)
    .LocalScaleX(1.0f)
    .LocalScaleY(1.0f)
    .Identity()
    .PositionIdentity()
    .RotationIdentity()
    .Position(Vector3.zero)
    .PositionX(0)
    .PositionY(0)
    .PositionZ(0)
    .Rotation(Quaternion.identity)
    .DestroyChildren()
    .AsLastSibling()
    .AsFirstSibling()
    .SiblingIndex(0);

selfScript
    .Parent(null)
    .LocalIdentity()
    .LocalPositionIdentity()
    .LocalRotationIdentity()
    .LocalScaleIdentity()
    .LocalPosition(Vector3.zero)
    .LocalPosition(0, 0, 0)
    .LocalPosition(0, 0)
    .LocalPositionX(0)
    .LocalPositionY(0)
    .LocalPositionZ(0)
    .LocalRotation(Quaternion.identity)
    .LocalScale(Vector3.one)
    .LocalScaleX(1.0f)
    .LocalScaleY(1.0f)
    .Identity()
    .PositionIdentity()
    .RotationIdentity()
    .Position(Vector3.zero)
    .PositionX(0)
    .PositionY(0)
    .PositionZ(0)
    .Rotation(Quaternion.identity)
    .DestroyChildren()
    .AsLastSibling()
    .AsFirstSibling()
    .SiblingIndex(0);
```

**Methods / 方法**

##### T AsFirstSibling<T>(T selfComponent)

- 描述: component.transform.SetAsFirstSibling()
- Description: component.transform.SetAsFirstSibling()

```csharp
component.AsFirstSibling();
```

##### GameObject AsFirstSibling(GameObject selfComponent)

- 描述: gameObj.transform.SetAsFirstSibling()
- Description: gameObj.transform.SetAsFirstSibling()

```csharp
gameObj.AsFirstSibling();
```

##### T AsLastSibling<T>(T selfComponent)

- 描述: component.transform.SetAsLastSibling()
- Description: component.transform.SetAsLastSibling()

```csharp
myScript.AsLastSibling();
```

##### GameObject AsLastSibling(GameObject self)

- 描述: gameObj.transform.SetAsLastSibling()
- Description: gameObj.transform.SetAsLastSibling()

```csharp
gameObj.AsLastSibling();
```

##### GameObject AsRootGameObject<T>(GameObject self)

- 描述: gameObject.transform.SetParent(null)
- Description: gameObject.transform.SetParent(null)

```csharp
gameObject.AsRootGameObject();
```

##### T AsRootTransform<T>(T self)

- 描述: component.transform.SetParent(null)
- Description: component.transform.SetParent(null)

```csharp
component.AsRootTransform();
```

##### T DestroyChildren<T>(T selfComponent)

- 描述: Destroy 掉所有的子 GameObject
- Description: destroy all child gameObjects

```csharp
rootTransform.DestroyChildren();
```

##### GameObject DestroyChildren(GameObject selfGameObj)

- 描述: Destroy 掉所有的子 GameObject
- Description: destroy all child gameObjects

```csharp
rootGameObj.DestroyChildren();
```

##### T DestroyChildrenWithCondition<T>(T selfComponent, Func<Transform, bool> condition)

- 描述: 根据条件 Destroy 掉所有的子 GameObject 
- Description: destroy all child gameObjects if condition matched

```csharp
rootTransform.DestroyChildrenWithCondition(child=>child != other);
```

##### T Identity<T>(T selfComponent)

- 描述: 设置世界位置:0 角度:0 缩放:1
- Description: set world pos:0 rotation:0 scale:1

```csharp
component.Identity();
```

##### GameObject Identity(GameObject self)

- 描述: 设置世界位置:0 角度:0 缩放:1
- Description: set world pos:0 rotation:0 scale:1

```csharp
component.Identity();
```

##### T LocalIdentity<T>(T self)

- 描述: 设置本地位置为 0、本地角度为 0、本地缩放为 1
- Description: set local pos:0 local angle:0 local scale:1

```csharp
myScript.LocalIdentity();
```

##### GameObject LocalIdentity(GameObject self)

- 描述: 设置 gameObject 的本地位置为 0、本地角度为 0、本地缩放为 1
- Description: set gameObject's local pos:0  local angle:0  local scale:1

```csharp
myScript.LocalIdentity();
```

##### Vector3 LocalPosition<T>(T selfComponent)

- 描述: return component.transform.localPosition
- Description: return component.transform.localPosition

```csharp
var localPosition = spriteRenderer.LocalPosition();
```

##### Vector3 LocalPosition(GameObject self)

- 描述: return gameObject.transform.localPosition
- Description: return gameObject.transform.localPosition

```csharp
Debug.Log(new GameObject().LocalPosition());
```

##### T LocalPosition<T>(T selfComponent, Vector3 localPos)

- 描述: component.transform.localPosition = localPosition
- Description: component.transform.localPosition = localPosition

```csharp
spriteRenderer.LocalPosition(new Vector3(0,100,0));
```

##### GameObject LocalPosition(GameObject self, Vector3 localPos)

- 描述: gameObject.transform.localPosition = localPosition
- Description: gameObject.transform.localPosition = localPosition

```csharp
new GameObject().LocalPosition(new Vector3(0,100,0));
```

##### T LocalPosition<T>(T selfComponent, float x, float y)

- 描述: component.transform.localPosition = new Vector3(x,y,component.transform.localPosition.z)
- Description: component.transform.localPosition = new Vector3(x,y,component.transform.localPosition.z)

```csharp
myScript.LocalPosition(0,0);
```

##### GameObject LocalPosition(GameObject self, float x, float y)

- 描述: gameObj.transform.localPosition = new Vector3(x,y,gameObj.transform.localPosition.z)
- Description: gameObj.transform.localPosition = new Vector3(x,y,gameObj.transform.localPosition.z)

```csharp
new GameObject().LocalPosition(0,0);
```

##### T LocalPosition<T>(T selfComponent, float x, float y, float z)

- 描述: component.transform.localPosition = new Vector3(x,y,z)
- Description: component.transform.localPosition = new Vector3(x,y,z)

```csharp
myScript.LocalPosition(0,0,-10);
```

##### GameObject LocalPosition(GameObject self, float x, float y, float z)

- 描述: gameObj.transform.localPosition = new Vector3(x,y,z)
- Description: gameObj.transform.localPosition = new Vector3(x,y,z)

```csharp
new GameObject().LocalPosition(0,0,-10);
```

##### T LocalPositionIdentity<T>(T selfComponent)

- 描述: component.transform.localPosition = Vector3.zero
- Description: component.transform.localPosition = Vector3.zero

```csharp
component.LocalPositionIdentity();
```

##### GameObject LocalPositionIdentity(GameObject self)

- 描述: gameObj.transform.localPosition = Vector3.zero
- Description: gameObj.transform.localPosition = Vector3.zero

```csharp
gameObj.LocalPositionIdentity();
```

##### T LocalPositionX<T>(T selfComponent, float x)

- 描述: component.transform.localPosition.x = x
- Description: component.transform.localPosition.x = x

```csharp
component.LocalPositionX(10);
```

##### GameObject LocalPositionX(GameObject self, float x)

- 描述: gameObj.transform.localPosition.x = x
- Description: gameObj.transform.localPosition.x = x

```csharp
gameObj.LocalPositionX(10);
```

##### T LocalPositionY<T>(T selfComponent, float y)

- 描述: component.transform.localPosition.y = y
- Description: component.transform.localPosition.y = y

```csharp
component.LocalPositionY(10);
```

##### GameObject LocalPositionY(GameObject selfComponent, float y)

- 描述: gameObj.transform.localPosition.y = y
- Description: gameObj.transform.localPosition.y = y

```csharp
gameObj.LocalPositionY(10);
```

##### T LocalPositionZ<T>(T selfComponent, float z)

- 描述: component.transform.localPosition.z = z
- Description: component.transform.localPosition.z = z

```csharp
component.LocalPositionZ(10);
```

##### GameObject LocalPositionZ(GameObject self, float z)

- 描述: gameObj.transform.localPosition.z = z
- Description: gameObj.transform.localPosition.z = z

```csharp
gameObj.LocalPositionZ(10);
```

##### Quaternion LocalRotation<T>(T selfComponent)

- 描述: return component.transform.localRotation
- Description: return component.transform.localRotation

```csharp
var localRotation = myScript.LocalRotation();
```

##### Quaternion LocalRotation(GameObject self)

- 描述: return gameObj.transform.localRotation
- Description: return gameObj.transform.localRotation

```csharp
var localRotation = gameObj.LocalRotation();
```

##### T LocalRotation<T>(T selfComponent, Quaternion localRotation)

- 描述: component.transform.localRotation = localRotation
- Description: component.transform.localRotation = localRotation

```csharp
myScript.LocalRotation(Quaternion.identity);
```

##### GameObject LocalRotation(GameObject selfComponent, Quaternion localRotation)

- 描述: gameObj.transform.localRotation = localRotation
- Description: gameObj.transform.localRotation = localRotation

```csharp
gameObj.LocalRotation(Quaternion.identity);
```

##### T LocalRotationIdentity<T>(T selfComponent)

- 描述: component.transform.localRotation = Quaternion.identity
- Description: component.transform.localRotation = Quaternion.identity

```csharp
component.LocalRotationIdentity();
```

##### GameObject LocalRotationIdentity(GameObject selfComponent)

- 描述: gameObj.transform.localRotation = Quaternion.identity
- Description: gameObj.transform.localRotation = Quaternion.identity

```csharp
gameObj.LocalRotationIdentity();
```

##### Vector3 LocalScale<T>(T selfComponent)

- 描述: return component.transform.localScale
- Description: return component.transform.localScale

```csharp
var localScale = myScript.LocalScale();
```

##### Vector3 LocalScale(GameObject self)

- 描述: return gameObj.transform.localScale
- Description: return gameObj.transform.localScale

```csharp
var localScale = gameObj.LocalScale();
```

##### T LocalScale<T>(T selfComponent, Vector3 scale)

- 描述: component.transform.localScale = scale
- Description: component.transform.localScale = scale

```csharp
component.LocalScale(Vector3.one);
```

##### GameObject LocalScale(GameObject self, Vector3 scale)

- 描述: gameObj.transform.localScale = scale
- Description: gameObj.transform.localScale = scale

```csharp
gameObj.LocalScale(Vector3.one);
```

##### T LocalScale<T>(T selfComponent, float xyz)

- 描述: component.transform.localScale = new Vector3(xyz,xyz,xyz)
- Description: component.transform.localScale = new Vector3(xyz,xyz,xyz)

```csharp
myScript.LocalScale(1);
```

##### GameObject LocalScale(GameObject self, float xyz)

- 描述: gameObj.transform.localScale = new Vector3(scale,scale,scale)
- Description: gameObj.transform.localScale = new Vector3(scale,scale,scale)

```csharp
gameObj.LocalScale(1);
```

##### T LocalScale<T>(T selfComponent, float x, float y)

- 描述: component.transform.localScale = new Vector3(x,y,component.transform.localScale.z)
- Description: component.transform.localScale = new Vector3(x,y,component.transform.localScale.z)

```csharp
component.LocalScale(2,2);
```

##### GameObject LocalScale(GameObject selfComponent, float x, float y)

- 描述: gameObj.transform.localScale = new Vector3(x,y,gameObj.transform.localScale.z)
- Description: gameObj.transform.localScale = new Vector3(x,y,gameObj.transform.localScale.z)

```csharp
gameObj.LocalScale(2,2);
```

##### T LocalScale<T>(T selfComponent, float x, float y, float z)

- 描述: component.transform.localScale = new Vector3(x,y,z)
- Description: component.transform.localScale = new Vector3(x,y,z)

```csharp
myScript.LocalScale(2,2,2);
```

##### GameObject LocalScale(GameObject selfComponent, float x, float y, float z)

- 描述: gameObj.transform.localScale = new Vector3(x,y,z)
- Description: gameObj.transform.localScale = new Vector3(x,y,z)

```csharp
gameObj.LocalScale(2,2,2);
```

##### T LocalScaleIdentity<T>(T selfComponent)

- 描述: component.transform.localScale = Vector3.one
- Description: component.transform.localScale = Vector3.one)

```csharp
component.LocalScaleIdentity();
```

##### GameObject LocalScaleIdentity(GameObject selfComponent)

- 描述: component.transform.localScale = Vector3.one
- Description: component.transform.localScale = Vector3.one)

```csharp
component.LocalScaleIdentity();
```

##### float LocalScaleX(GameObject self)

- 描述: gameObj.transform.localScale.x
- Description: gameObj.transform.localScale.x)

```csharp
var scaleX = gameObj.LocalScaleX();
```

##### float LocalScaleX<T>(T self)

- 描述: component.transform.localScale.x
- Description: component.transform.localScale.x)

```csharp
var scaleX = component.LocalScaleX();
```

##### T LocalScaleX<T>(T selfComponent, float x)

- 描述: component.transform.localScale.x = x
- Description: component.transform.localScale.x = x)

```csharp
component.LocalScaleX(10);
```

##### GameObject LocalScaleX(GameObject self, float x)

- 描述: gameObj.transform.localScale.x = x
- Description: gameObj.transform.localScale.x = x)

```csharp
gameObj.LocalScaleX(10);
```

##### float LocalScaleY<T>(T self)

- 描述: component.transform.localScale.y
- Description: component.transform.localScale.y)

```csharp
var scaleY = component.LocalScaleY(10);
```

##### float LocalScaleY(GameObject self)

- 描述: gameObj.transform.localScale.y
- Description: gameObj.transform.localScale.y)

```csharp
var scaleY = gameObj.LocalScaleY();
```

##### T LocalScaleY<T>(T self, float y)

- 描述: component.transform.localScale.y = y
- Description: component.transform.localScale.y = y)

```csharp
component.LocalScaleY(10);
```

##### GameObject LocalScaleY(GameObject self, float y)

- 描述: gameObj.transform.localScale.y = y
- Description: gameObj.transform.localScale.y = y)

```csharp
gameObj.LocalScaleY(10);
```

##### float LocalScaleZ<T>(T self)

- 描述: component.transform.localScale.z
- Description: component.transform.localScale.z)

```csharp
var scaleZ = component.LocalScaleZ();
```

##### float LocalScaleZ(GameObject self)

- 描述: gameObj.transform.localScale.z
- Description: gameObj.transform.localScale.z)

```csharp
var scaleZ = gameObj.LocalScaleZ();
```

##### T LocalScaleZ<T>(T selfComponent, float z)

- 描述: component.transform.localScale.z = z
- Description: component.transform.localScale.z = z)

```csharp
component.LocalScaleZ(10);
```

##### GameObject LocalScaleZ(GameObject selfComponent, float z)

- 描述: gameObj.transform.localScale.z = z
- Description: gameObj.transform.localScale.z = z)

```csharp
gameObj.LocalScaleZ(10);
```

##### T Parent<T>(T self, Component parent)

- 描述: component.transform.SetParent(parent)
- Description: component.transform.SetParent(parent)

```csharp
myScript.Parent(rootGameObj);
```

##### GameObject Parent(GameObject self, Component parent)

- 描述: gameObject.transform.SetParent(parent)
- Description: gameObject.transform.SetParent(parent)

```csharp
gameObj.SetParent(null);
```

##### Vector3 Position<T>(T selfComponent)

- 描述: return component.transform.position
- Description: return component.transform.position

```csharp
var pos = myScript.Position();
```

##### Vector3 Position(GameObject self)

- 描述: return gameObj.transform.position
- Description: return gameObj.transform.position

```csharp
var pos = gameObj.Position();
```

##### T Position<T>(T selfComponent, Vector3 position)

- 描述: component.transform.position = position
- Description: component.transform.position = position

```csharp
component.Position(Vector3.zero);
```

##### GameObject Position(GameObject self, Vector3 position)

- 描述: gameObj.transform.position = position
- Description: gameObj.transform.position = position

```csharp
gameObj.Position(Vector3.zero);
```

##### T Position<T>(T selfComponent, float x, float y)

- 描述: component.transform.position = new Vector3(x,y,原来的 z)
- Description: component.transform.position = new Vector3(x,y,origin z)

```csharp
component.Position(0,0);
```

##### GameObject Position(GameObject selfComponent, float x, float y)

- 描述: gameObj.transform.position = new Vector3(x,y,原来的 z)
- Description: gameObj.transform.position = new Vector3(x,y,origin z)

```csharp
gameObj.Position(0,0,-10);
```

##### T Position<T>(T selfComponent, float x, float y, float z)

- 描述: component.transform.position = new Vector3(x,y,z)
- Description: component.transform.position = new Vector3(x,y,z)

```csharp
myScript.Position(0,0,-10);
```

##### GameObject Position(GameObject self, float x, float y, float z)

- 描述: gameObj.transform.position = new Vector3(x,y,z)
- Description: gameObj.transform.position = new Vector3(x,y,z)

```csharp
gameObj.Position(0,0,-10);
```

##### T PositionIdentity<T>(T selfComponent)

- 描述: component.transform.position = Vector3.zero
- Description: component.transform.position = Vector3.zero

```csharp
component.PositionIdentity();
```

##### GameObject PositionIdentity(GameObject selfComponent)

- 描述: gameObj.transform.position = Vector3.zero
- Description: gameObj.transform.position = Vector3.zero

```csharp
gameObj.PositionIdentity();
```

##### T PositionX<T>(T selfComponent, float x)

- 描述: component.transform.position.x = x
- Description: component.transform.position.x = x

```csharp
component.PositionX(x);
```

##### GameObject PositionX(GameObject self, float x)

- 描述: gameObj.transform.position.x = x
- Description: gameObj.transform.position.x = x

```csharp
gameObj.PositionX(x);
```

##### T PositionX<T>(T selfComponent, Func<float, float> xSetter)

- 描述: 将 positionX 的计算结果设置给 position.x
- Description: Sets the positionX calculation to position.x

```csharp
component.PositionX(x=>x * 5);
```

##### GameObject PositionX(GameObject self, Func<float, float> xSetter)

- 描述: 将 positionX 的计算结果设置给 position.x
- Description: Sets the positionX calculation to position.x

```csharp
gameObj.PositionX(x=>x * 5);
```

##### T PositionY<T>(T selfComponent, float y)

- 描述: component.transform.position.y = y
- Description: component.transform.position.y = y

```csharp
myScript.PositionY(10);
```

##### GameObject PositionY(GameObject self, float y)

- 描述: component.transform.position.y = y
- Description: component.transform.position.y = y

```csharp
myScript.PositionY(10);
```

##### T PositionY<T>(T selfComponent, Func<float, float> ySetter)

- 描述: 将 positionY 的计算结果设置给 position.y
- Description: Sets the positionY calculation to position.y

```csharp
component.PositionY(y=>y * 5);
```

##### GameObject PositionY(GameObject self, Func<float, float> ySetter)

- 描述: 将 positionY 的计算结果设置给 position.y
- Description: Sets the positionY calculation to position.y

```csharp
gameObj.PositionY(y=>y * 5);
```

##### T PositionZ<T>(T selfComponent, float z)

- 描述: component.transform.position.z = z
- Description: component.transform.position.z = z

```csharp
component.PositionZ(10);
```

##### GameObject PositionZ(GameObject self, float z)

- 描述: component.transform.position.z = z
- Description: component.transform.position.z = z

```csharp
component.PositionZ(10);
```

##### T PositionZ<T>(T self, Func<float, float> zSetter)

- 描述: 将 positionZ 的计算结果设置给 position.z
- Description: Sets the positionZ calculation to position.z

```csharp
component.PositionZ(z=>z * 5);
```

##### GameObject PositionZ(GameObject self, Func<float, float> zSetter)

- 描述: 将 positionZ 的计算结果设置给 position.z
- Description: Sets the positionZ calculation to position.z

```csharp
component.PositionZ(z=>z * 5);
```

##### Quaternion Rotation<T>(T selfComponent)

- 描述: return component.transform.rotation
- Description: return component.transform.rotation

```csharp
var rotation = myScript.Rotation();
```

##### Quaternion Rotation(GameObject self)

- 描述: return gameObj.transform.rotation
- Description: return gameObj.transform.rotation

```csharp
var rotation = gameObj.Rotation();
```

##### T Rotation<T>(T selfComponent, Quaternion rotation)

- 描述: component.transform.rotation = rotation
- Description: component.transform.rotation = rotation

```csharp
component.Rotation(Quaternion.identity);
```

##### GameObject Rotation(GameObject self, Quaternion rotation)

- 描述: gameObj.transform.rotation = rotation
- Description: gameObj.transform.rotation = rotation

```csharp
gameObj.Rotation(Quaternion.identity);
```

##### T RotationIdentity<T>(T selfComponent)

- 描述: component.transform.rotation = Quaternion.identity
- Description: component.transform.rotation = Quaternion.identity

```csharp
component.RotationIdentity();
```

##### GameObject RotationIdentity(GameObject selfComponent)

- 描述: gameObj.transform.rotation = Quaternion.identity
- Description: gameObj.transform.rotation = Quaternion.identity

```csharp
gameObj.RotationIdentity();
```

##### Vector3 Scale<T>(T selfComponent)

- 描述: return component.transform.lossyScale
- Description: return component.transform.lossyScale

```csharp
var scale = component.Scale();
```

##### Vector3 Scale(GameObject selfComponent)

- 描述: return gameObj.transform.lossyScale
- Description: return gameObj.transform.lossyScale

```csharp
var scale = gameObj.Scale();
```

##### T SiblingIndex<T>(T selfComponent, int index)

- 描述: component.transform.SetSiblingIndex(index)
- Description: component.transform.SetSiblingIndex(index)

```csharp
myScript.SiblingIndex(10);
```

##### GameObject SiblingIndex(GameObject selfComponent, int index)

- 描述: gameObj.transform.SetSiblingIndex(index)
- Description: gameObj.transform.SetSiblingIndex(index)

```csharp
gameObj.SiblingIndex(10);
```



---

#### UnityEngine.MonoBehaviour (UnityEngineMonoBehaviourExtension)

- Type: `QFramework.UnityEngineMonoBehaviourExtension`
- Namespace: `QFramework`

**Description / 描述**

- 描述: MonoBehaviour 静态扩展
- Description: MonoBehaviour extension

**Methods / 方法**

##### T Disable<T>(T selfBehaviour)

- 描述: monoBehaviour.enable = false
- Description: monoBehaviour.enable = false

```csharp
myScript.Disable();
```

##### T Enable<T>(T selfBehaviour, bool enable)

- 描述: monoBehaviour.enable = true
- Description: monoBehaviour.enable = true)

```csharp
myScript.Enable();
```



---

#### UnityEngine.Camera (UnityEngineCameraExtension)

- Type: `QFramework.UnityEngineCameraExtension`
- Namespace: `QFramework`

**Description / 描述**

- 描述: UnityEngine.Camera 静态扩展
- Description: UnityEngine.Camera extension

**Methods / 方法**

##### Texture2D CaptureCamera(Camera camera, Rect rect)

- 描述: 截图
- Description: captureScreen

```csharp
Camera.main.CaptureCamera(new Rect(0, 0, Screen.width, Screen.height));
```



---

#### UnityEngine.Color (UnityEngineColorExtension)

- Type: `QFramework.UnityEngineColorExtension`
- Namespace: `QFramework`

**Description / 描述**

- 描述: UnityEngine.Color 静态扩展
- Description: UnityEngine.Color extension

**Methods / 方法**

##### Color HtmlStringToColor(string htmlString)

- 描述: HTML string(#000000) 转 Color
- Description: HTML string(like #000000)

```csharp
var color = "#C5563CFF".HtmlStringToColor();
Debug.Log(color);
```



---

#### UnityEngine.Graphic (UnityEngineUIGraphicExtension)

- Type: `QFramework.UnityEngineUIGraphicExtension`
- Namespace: `QFramework`

**Description / 描述**

- 描述: UnityEngine.UI.Graphic 静态扩展
- Description: UnityEngine.UI.Graphic extension

**Methods / 方法**

##### T ColorAlpha<T>(T selfGraphic, float alpha)

- 描述: 设置 Graphic 的 alpha 值 
- Description: set graphic's alpha value

```csharp
var gameObject = new GameObject();
var image = gameObject.AddComponent<Image>();
var rawImage = gameObject.AddComponent<RawImage>();


image.ColorAlpha(1.0f);
rawImage.ColorAlpha(1.0f);
```

##### Image FillAmount(Image selfImage, float fillAmount)

- 描述: 设置 image 的 fillAmount 值
- Description: set image's fillAmount value

```csharp
var gameObject = new GameObject();
var image1 = gameObject.AddComponent<Image>();

image1.FillAmount(0.0f);
```



---

#### UnityEngine.Random (RandomUtility)

- Type: `QFramework.RandomUtility`
- Namespace: `QFramework`

**Description / 描述**

- 描述: 针对随机做的一些封装
- Description: wrapper for random

**Methods / 方法**

##### T Choose<T>(T[] args)

- 描述: 随机选择
- Description: RandomChoose

```csharp
var result = RandomUtility.Choose(1,1,1,2,2,2,2,3,3);

if (result == 3)
{
    // todo ...
}
```



---

#### UnityEngine.Others (UnityEngineOthersExtension)

- Type: `QFramework.UnityEngineOthersExtension`
- Namespace: `QFramework`

**Description / 描述**

- 描述: 其他的一些静态扩展
- Description: other extension

**Methods / 方法**

##### float Abs(float self)

- 描述: Mathf.Abs
- Description: Mathf.Abs

```csharp
var absValue = -1.0f.Abs();
// absValue is 1.0f
```

##### SpriteRenderer Alpha(SpriteRenderer self, float alpha)

- 描述: 为 SpriteRender 设置 alpha 值
- Description: set SpriteRender's alpha value

```csharp
mySprRender.Alpha(0.5f);
```

##### Vector2 AngleToDirection2D(int self)

- 描述: 将欧拉角转换为方向向量(Vector2)
- Description: Convert Degree To Direction(Vector2)

```csharp
var direction = 90.AngleToDirection2D();
// Vector2(1,0)
```

##### float Cos(float self)

- 描述: Mathf.Cos
- Description: Mathf.Cos

```csharp
var cos = (90.0f * Mathf.Deg2Rad).Cos();
// cos is 0f
```

##### float CosAngle(float self)

- 描述: Mathf.Cos(x * Mathf.Deg2Rad)
- Description: Mathf.Cos(x * Mathf.Deg2Rad)

```csharp
var cos = 90.0f.CosAngle();
// cos is 0f
```

##### float Deg2Rad(float self)

- 描述: Mathf.Deg2Rad
- Description: Mathf.Deg2Rad

```csharp
var radius = 90.0f.Deg2Rad();
// radius is 1.57f
```

##### float Exp(float self)

- 描述: Mathf.Exp
- Description: Mathf.Exp

```csharp
var expValue = 1.0f.Exp(); // Mathf.Exp(1.0f)
```

##### T GetAndRemoveRandomItem<T>(List<T> list)

- 描述: 随机获取并删除 List 中的一个元素
- Description: get and remove random item in a list

```csharp
new List<int>(){ 1,2,3 }.GetAndRemoveRandomItem();
```

##### T GetRandomItem<T>(List<T> list)

- 描述: 随机 List 中的一个元素
- Description: get random item in a list

```csharp
new List<int>(){ 1,2,3 }.GetRandomItem();
```

##### float Lerp(float self, float a, float b)

- 描述: Mathf.Lerp
- Description: Mathf.Lerp

```csharp
var v = 0.5f.Lerp(0.1f,0.5f);
// v is 0.3f
```

##### float Rad2Deg(float self)

- 描述: Mathf.Rad2Deg
- Description: Mathf.Rad2Deg

```csharp
var degree = 1.57f.Rad2Deg();
// degree is 90f
```

##### float Sign(float self)

- 描述: Mathf.Sign
- Description: Mathf.Sign

```csharp
var sign = -5.0f.Sign();
// sign is 5.0f
```

##### float Sin(float self)

- 描述: Mathf.Sin
- Description: Mathf.Sin

```csharp
var sin = (90.0f * Mathf.Deg2Rad).Sin();
// sin is 1f
```

##### float SinAngle(float self)

- 描述: Mathf.Sin(x * Mathf.Deg2Rad)
- Description: Mathf.Sin(x * Mathf.Deg2Rad)

```csharp
var sin = 90.0f.SinAngle();
// sin is 1f
```

##### float ToAngle(Vector2 self)

- 描述: 将方向(Vector2)转换为欧拉角
- Description: Convert Direction To Degrees

```csharp
var direction = Vector2.right.ToAngle();
// Vector2(1,0)
```



---

#### UnityEngine.Vector2/3 (UnityEngineVectorExtension)

- Type: `QFramework.UnityEngineVectorExtension`
- Namespace: `QFramework`

**Description / 描述**

- 描述: 针对 Vector2/Vector3 封装的函数
- Description: wrapper function for Vector2/Vector3

**Example / 示例**

```csharp
gameObjA.DirectionFrom(gameObjB);
    myComponentA.DirectionFrom(gameObjB);
    gameObjA.DirectionFrom(myComponentB);
    myComponentA.DirectionFrom(myComponentB);

    // also support DirectionTo/ NormalizedDirectionFrom /NormalizedDirectionTo
```

**Methods / 方法**

##### Vector2 Direction2DFrom(Component self, Component from)

- 描述: (Vector2)(self.transform.position - from.transform.position)
- Description: (Vector2)(self.transform.position - from.transform.position)

```csharp
gameObj/otherComponent.Direction2DFrom(otherGameObj/otherComponent)
```

##### Vector2 Direction2DTo(Component self, Component to)

- 描述: (Vector2)(to.transform.position - self.transform.position)
- Description: (Vector2)(to.transform.position - self.transform.position)

```csharp
gameObj/otherComponent.Direction2DTo(otherGameObj/otherComponent)
```

##### Vector3 DirectionFrom(Component self, Component from)

- 描述: self.transform.position - from.transform.position
- Description: self.transform.position - from.transform.position

```csharp
gameObj/otherComponent.DirectionFrom(otherGameObj/otherComponent)
```

##### Vector3 DirectionTo(Component self, Component to)

- 描述: to.transform.position - self.transform.position
- Description: to.transform.position - self.transform.position

```csharp
gameObj/otherComponent.DirectionTo(otherGameObj/otherComponent)
```

##### Vector2 NormalizedDirection2DFrom(Component self, Component from)

- 描述: ((Vector2)(self.transform.position - from.transform.position)).normalized
- Description: ((Vector2)(self.transform.position - from.transform.position)).normalized

```csharp
gameObj/otherComponent.NormalizedDirection2DFrom(otherGameObj/otherComponent)
```

##### Vector2 NormalizedDirection2DTo(Component self, Component to)

- 描述: ((Vector2)(to.transform.position - self.transform.position)).normalized
- Description: ((Vector2)(to.transform.position - self.transform.position)).normalized

```csharp
gameObj/otherComponent.NormalizedDirection2DTo(otherGameObj/otherComponent)
```

##### Vector3 NormalizedDirectionFrom(Component self, Component from)

- 描述: (self.transform.position - from.transform.position).normalized
- Description: (self.transform.position - from.transform.position).normalized

```csharp
gameObj/otherComponent.NormalizedDirectionTo(otherGameObj/otherComponent)
```

##### Vector3 NormalizedDirectionTo(Component self, Component to)

- 描述: (to.transform.position - self.transform.position).normalized
- Description: (to.transform.position - self.transform.position).normalized

```csharp
gameObj/otherComponent.NormalizedDirectionTo(otherGameObj/otherComponent)
```



---

#### UnityEngine.RectTransform (UnityEngineRectTransformExtension)

- Type: `QFramework.UnityEngineRectTransformExtension`
- Namespace: `QFramework`

**Description / 描述**

- 描述: 针对 RectTransform 封装的函数
- Description: wrapper function for RectTransform

**Methods / 方法**

##### RectTransform AnchoredPositionY(RectTransform selfRectTrans, float anchoredPositionY)

- 描述: 设置 rectTransform.anchoredPosition.y 值
- Description: set rectTransform.anchoredPosition.y value

```csharp
text.rectTransform.AnchoredPositionY(5);
```



---

### API Group: 01.FluentAPI.CSharp

#### System.Object (SystemObjectExtension)

- Type: `QFramework.SystemObjectExtension`
- Namespace: `QFramework`

**Description / 描述**

- 描述: 针对 System.Object 提供的链式扩展，理论上任何对象都可以使用
- Description: The chain extension provided by System.object can theoretically be used by any Object

**Methods / 方法**

##### T As<T>(object selfObj)

- 描述: 转型
- Description: cast

```csharp
int a = 10;
Debug.Log(a.As<float>())
// 10
```

##### bool IsNotNull<T>(T selfObj)

- 描述: 判断不是为空
- Description: Check Is Not Null,return true or false

```csharp
var simpleObject = new object();
        
if (simpleObject.IsNotNull()) // simpleObject != null
{
    // do sth
}
```

##### bool IsNull<T>(T selfObj)

- 描述: 判断是否为空
- Description: Check Is Null,return true or false

```csharp
var simpleObject = new object();
        
if (simpleObject.IsNull()) // simpleObject == null
{
    // do sth
}
```

##### T Self<T>(T self, Action<T> onDo)

- 描述: 将自己传到 Action 委托中
- Description: apply self to the Action delegate

```csharp
new GameObject()
        .Self(gameObj=>gameObj.name = "Enemy")
        .Self(gameObj=>{
            Debug.Log(gameObj.name);
        });
```

##### T Self<T>(T self, Func<T, T> onDo)

- 描述: 将自己传到 Func<T,T> 委托中,然后返回自己
- Description: apply self to the Func<T,T> delegate

```csharp
new GameObject()
        .Self(gameObj=>gameObj.name = "Enemy")
        .Self(gameObj=>{
            Debug.Log(gameObj.name);
        });
```



---

#### System.String (SystemStringExtension)

- Type: `QFramework.SystemStringExtension`
- Namespace: `QFramework`

**Description / 描述**

- 描述: 针对 System.String 提供的链式扩展，理论上任何集合都可以使用
- Description: The chain extension provided by System.Collections can theoretically be used by any collection

**Methods / 方法**

##### StringBuilder AddPrefix(StringBuilder self, string prefixString)

- 描述: StringBuilder 添加前缀
- Description: StringBuilder insert prefix string

```csharp
var builder = "I'm liangxie".Builder().AddPrefix("Hi!") ;
Debug.Log(builder.ToString());
// Hi!I'm liangxie
```

##### StringBuilder Builder(string selfStr)

- 描述: 返回包含此字符串的 StringBuilder
- Description: Returns a StringBuilder containing this string

```csharp
var builder = "Hello".Builder();
builder.Append(" QF");
Debug.Log(builder.ToString());
// Hello QF
```

##### string FillFormat(string selfStr, object[] args)

- 描述: 格式化字符串填充参数
- Description: The format string populates the parameters

```csharp
var newStr = "{0},{1}".FillFormat(1,2);
Debug.Log(newStr);
// 1,2
```

##### bool HasChinese(string input)

- 描述: 是否存在中文字符
- Description: check string contains chinese or not

```csharp
Debug.Log("你好".HasChinese());
// true
```

##### bool HasSpace(string input)

- 描述: 是否存在空格
- Description: check string contains space or not

```csharp
Debug.Log("你好 ".HasSpace());
// true
```

##### bool IsNotNullAndEmpty(string selfStr)

- 描述: 检测是否为非空且非Empty
- Description: Checks both not null and non-empty

```csharp
Debug.Log("Hello".IsNotNullAndEmpty());
// true
```

##### bool IsNullOrEmpty(string selfStr)

- 描述: 检测是否为空或 Empty
- Description: Check Whether string is null or empty

```csharp
Debug.Log(string.Empty.IsNullOrEmpty());
// true
```

##### bool IsTrimNotNullAndEmpty(string selfStr)

- 描述: 去掉两端空格后，检测是否为非 null 且非 Empty
- Description: After removing whitespace from both sides, check whether it is non-null and non-empty

```csharp
Debug.Log("  123  ".IsTrimNotNullAndEmpty());
// true
```

##### bool IsTrimNullOrEmpty(string selfStr)

- 描述: 去掉两端空格后，检测是否为空或 Empty
- Description: Check if it is Empty or Empty after removing whitespace from both sides

```csharp
Debug.Log("   ".IsTrimNullOrEmpty());
// true
```

##### string RemoveString(string str, string[] targets)

- 描述: remove string
- Description: check string contains space or not

```csharp
Debug.Log("Hello World ".RemoveString("Hello"," "));
// World
```

##### string[] Split(string selfStr, Char splitSymbol)

- 描述: 字符串分割
- Description: String splitting

```csharp
"1.2.3.4.5".Split('.').ForEach(str=>Debug.Log(str));
// 1 2 3 4 5
```

##### string StringJoin(IEnumerable<string> self, string separator)

- 描述: join string
- Description: join string

```csharp
Debug.Log(new List<string>() { "1","2","3"}.StringJoin(","));
// 1,2,3
```

##### DateTime ToDateTime(string selfStr, DateTime defaultValue)

- 描述: 字符串解析成 Int
- Description: parse string to int

```csharp
DateTime.Now.ToString().ToDataTime();
```

##### float ToFloat(string selfStr, float defaultValue)

- 描述: 字符串解析成 float
- Description: parse string to float

```csharp
var number = "123456f".ToInt();
Debug.Log(number);
// 123456
// notice unsafe
// 不安全
```

##### int ToInt(string selfStr, int defaulValue)

- 描述: 字符串解析成 Int
- Description: parse string to int

```csharp
var number = "123456".ToInt();
Debug.Log(number);
// 123456
// notice unsafe
// 不安全
```



---

#### System.IO (SystemIOExtension)

- Type: `QFramework.SystemIOExtension`
- Namespace: `QFramework`

**Description / 描述**

- 描述: 针对 System.IO 提供的链式扩展,主要是文件和文件夹的一些 IO 操作
- Description: IO chain extension for system. IO, mainly file and folder IO operations

**Methods / 方法**

##### string CombinePath(string selfPath, string toCombinePath)

- 描述: 合并路径
- Description: Combine path

```csharp
var path = Application.dataPath.CombinePath("Resources");
Debug.Log(Path)
// projectPath/Assets/Resources
```

##### string CreateDirIfNotExists(string dirFullPath)

- 描述: 创建文件夹,如果存在则不创建
- Description: Create folder or not if it exists

```csharp
var testDir = "Assets/TestFolder";
testDir.CreateDirIfNotExists();
```

##### void DeleteDirIfExists(string dirFullPath)

- 描述: 删除文件夹，如果存在
- Description: Delete the folder if it exists

```csharp
var testDir ="Assets/TestFolder";
testDir.DeleteDirIfExists();
```

##### bool DeleteFileIfExists(string fileFullPath)

- 描述: 删除文件 如果存在
- Description: Delete the file if it exists

```csharp
var filePath = "Assets/Test.txt";
File.Create("Assets/Test");
filePath.DeleteFileIfExists();
```

##### void EmptyDirIfExists(string dirFullPath)

- 描述: 清空 Dir（保留目录),如果存在
- Description: Clear Dir (reserved directory), if exists

```csharp
var testDir = "Assets/TestFolder";
testDir.EmptyDirIfExists();
```

##### string GetFileExtendName(string filePath)

- 描述: 根据路径获取文件扩展名
- Description: Get the file extension based on the path

```csharp
var fileName ="/abc/def/b.txt".GetFileExtendName();
Debug.Log(fileName0);
// .txt
```

##### string GetFileName(string filePath)

- 描述: 根据路径获取文件名
- Description: get file name by path

```csharp
var fileName ="/abc/def/b.txt".GetFileName();
Debug.Log(fileName0);
// b.txt
```

##### string GetFileNameWithoutExtend(string filePath)

- 描述: 根据路径获取文件名，不包含文件扩展名
- Description: Get the file name based on the path, excluding the file name extension

```csharp
var fileName ="/abc/def/b.txt".GetFileNameWithoutExtend();
Debug.Log(fileName0);
// b
```

##### string GetFolderPath(string path)

- 描述: 获取文件夹路径
- Description: get filePath's folder path

```csharp
var folderPath ="/abc/def/b.txt".GetFolderPath();
Debug.Log(fileName0);
// /abs/def
```



---

#### System.Collections (CollectionsExtension)

- Type: `QFramework.CollectionsExtension`
- Namespace: `QFramework`

**Description / 描述**

- 描述: 针对 System.Collections 提供的链式扩展，理论上任何集合都可以使用
- Description: The chain extension provided by System.Collections can theoretically be used by any collection

**Methods / 方法**

##### void AddRange<K, V>(Dictionary<K, V> dict, Dictionary<K, V> addInDict, bool isOverride)

- 描述: 字典添加新的字典
- Description: Dictionary Adds a new dictionary

```csharp
var dictionary1 = new Dictionary<string, string> { { "1", "2" } };
var dictionary2 = new Dictionary<string, string> { { "1", "4" } };
var dictionary3 = dictionary1.AddRange(dictionary2,true); // true means override
dictionary3.ForEach(pair => Debug.LogFormat("{0}:{1}", pair.Key, pair.Value));
// 1:2
// 3:4

// notice: duplicate keys are  supported.
// 注意：支持重复的 key。
```

##### IEnumerable<T> ForEach<T>(IEnumerable<T> self, Action<T> action)

- 描述: 遍历 IEnumerable
- Description: ForEach for IEnumerable

```csharp
IEnumerable<int> testIEnumerable = new List<int> { 1, 2, 3 };
testIEnumerable.ForEach(number => Debug.Log(number));
// output 
// 1 
// 2 
// 3
new Dictionary<string, string>() 
{ 
    {"name","liangxie"}, 
    {"company","liangxiegame" } 
}
.ForEach(keyValue => Debug.LogFormat("key:{0},value:{1}", keyValue.Key, keyValue.Value));
// key:name,value:liangxie
// key:company,value:liangxiegame
```

##### void ForEach<T>(List<T> list, Action<int, T> action)

- 描述: 遍历 List (可获得索引）
- Description: foreach List (can get index)

```csharp
var testList = new List<string> {"a", "b", "c" };
testList.Foreach((c,index)=>Debug.Log(index)); 
// 1, 2, 3,
```

##### void ForEach<K, V>(Dictionary<K, V> dict, Action<K, V> action)

- 描述: 遍历字典
- Description: ForEach Dictionary

```csharp
var infos = new Dictionary<string,string> {{"name","liangxie"},{"age","18"}};
infos.ForEach((key,value)=> Debug.LogFormat("{0}:{1}",key,value);
// name:liangxie    
// age:18
```

##### List<T> ForEachReverse<T>(List<T> selfList, Action<T> action)

- 描述: List 倒序遍历
- Description: Reverse ForEach for List

```csharp
var testList = new List<int> { 1, 2, 3 };
testList.ForEachReverse(number => number.LogInfo());
// 3 2 1
```

##### Dictionary<TKey, TValue> Merge<TKey, TValue>(Dictionary<TKey, TValue> dictionary, Dictionary<TKey, TValue>[] dictionaries)

- 描述: 合并字典
- Description: Merge Dictionaries

```csharp
var dictionary1 = new Dictionary<string, string> { { "1", "2" } };
var dictionary2 = new Dictionary<string, string> { { "3", "4" } };
var dictionary3 = dictionary1.Merge(dictionary2);
dictionary3.ForEach(pair => Debug.LogFormat("{0}:{1}", pair.Key, pair.Value));
// 1:2
// 3:4

// notice: duplicate keys are not supported.
// 注意：不支持重复的 key。
```



---

#### System.Reflection (SystemReflectionExtension)

- Type: `QFramework.SystemReflectionExtension`
- Namespace: `QFramework`

**Description / 描述**

- 描述: 针对 System.Reflection 提供的链式扩展
- Description: Chain extension provided for System.Reflection

**Methods / 方法**

##### T CreateInstance<T>(Type self)

- 描述: 通过 Type 创建 Instance
- Description: Create Instance By Type

```csharp
interface IA
{

}

class A
{
}

IA a = typeof(A).CreateInstance<IA>();
```

##### T GetAttribute<T>(Type type, bool inherit)

- 描述: 获取指定的 Attribute
- Description: Gets the specified Attribute

```csharp
[DisplayName("A Class")
class A
{
    [DisplayName("A Number")
    public int Number;

    [DisplayName("Is Complete?")
    private bool Complete => Number > 100;

    [DisplayName("Say complete result?")
    public void SayComplete()
    {
        Debug.Log(Complete);
    }
}

var aType = typeof(A);
//
Debug.Log(aType.GetAttribute(typeof(DisplayNameAttribute));
// DisplayNameAttribute
Debug.Log(aType.GetAttribute<DisplayNameAttribute>());
// DisplayNameAttribute

// also support MethodInfo、PropertyInfo、FieldInfo
// 同时 也支持 MethodInfo、PropertyInfo、FieldInfo
```

##### bool HasAttribute<T>(Type type, bool inherit)

- 描述: 检查是否有指定的 Attribute
- Description: Check whether the specified Attribute exists

```csharp
[DisplayName("A Class")
class A
{
    [DisplayName("A Number")
    public int Number;

    [DisplayName("Is Complete?")
    private bool Complete => Number > 100;

    [DisplayName("Say complete result?")
    public void SayComplete()
    {
        Debug.Log(Complete);
    }
}

var aType = typeof(A);
//
Debug.Log(aType.HasAttribute(typeof(DisplayNameAttribute));
// true
Debug.Log(aType.HasAttribute<DisplayNameAttribute>());
// true

// also support MethodInfo、PropertyInfo、FieldInfo
// 同时 也支持 MethodInfo、PropertyInfo、FieldInfo
```

##### object ReflectionCallPrivateMethod<T>(T self, string methodName, object[] args)

- 描述: 通过反射的方式调用私有方法
- Description: call private method by reflection

```csharp
class A
{
    private void Say() { Debug.Log("I'm A!") }
}

new A().ReflectionCallPrivateMethod("Say");
// I'm A!
```

##### TReturnType ReflectionCallPrivateMethod<T, TReturnType>(T self, string methodName, object[] args)

- 描述: 通过反射的方式调用私有方法，有返回值
- Description: call private method by reflection,return the result

```csharp
class A
{
    private bool Add(int a,int b) { return a + b; }
}

Debug.Log(new A().ReflectionCallPrivateMethod("Add",1,2));
// 3
```



---

### API Group: 02.LogKit

#### LogKit (LogKit)

- Type: `QFramework.LogKit`
- Namespace: `QFramework`

**Description / 描述**

- 描述: 简单的日志工具
- Description: Simple Log ToolKit

**Properties / 属性**

##### LogLevel Level

- 描述: 日志等级设置
- Description: log level

```csharp
LogKit.Level = LogKit.LogLevel.None;
LogKit.I("LogKit"); // no output
LogKit.Level = LogKit.LogLevel.Exception;
LogKit.Level = LogKit.LogLevel.Error;
LogKit.Level = LogKit.LogLevel.Warning;
LogKit.Level = LogKit.LogLevel.Normal;
LogKit.I("LogKit"); 
// LogKit
LogKit.Level = LogKit.LogLevel.Max;
```


**Methods / 方法**

##### StringBuilder Builder()

- 描述: 获得 StringBuilder 用来拼接日志
- Description: get stringBuilder for generate log string

```csharp
LogKit.Builder()
    .Append("Hello")
    .Append(" LogKit")
    .ToString()
    .LogInfo();
// Hello LogKit
```

##### void E(Exception e)

- 描述: Debug.LogException
- Description: Debug.LogException

```csharp
LogKit.E("Hello LogKit");
// Hello LogKit
LogKit.E("Hello LogKit {0}{1}",1,2);
// Hello LogKit 12
"Hello LogKit FluentAPI".LogError();
// Hello LogKit FluentAPI
```

##### void E(object msg, object[] args)

- 描述: Debug.LogError & Debug.LogErrorFormat
- Description: Debug.LogError & Debug.LogErrorFormat

```csharp
LogKit.E("Hello LogKit");
// Hello LogKit
LogKit.E("Hello LogKit {0}{1}",1,2);
// Hello LogKit 12
"Hello LogKit FluentAPI".LogError();
// Hello LogKit FluentAPI
```

##### void I(object msg, object[] args)

- 描述: Debug.Log & Debug.LogFormat
- Description: Debug.Log & Debug.LogFormat

```csharp
LogKit.I("Hello LogKit");
// Hello LogKit
LogKit.I("Hello LogKit {0}{1}",1,2);
// Hello LogKit 12
"Hello LogKit FluentAPI".LogInfo();
// Hello LogKit FluentAPI
```

##### void W(object msg, object[] args)

- 描述: Debug.LogWarning & Debug.LogWaringFormat
- Description: Debug.LogWarning & Debug.LogWarningFormat

```csharp
LogKit.E("Hello LogKit");
// Hello LogKit
LogKit.E("Hello LogKit {0}{1}",1,2);
// Hello LogKit 12
"Hello LogKit FluentAPI".LogError();
// Hello LogKit FluentAPI
```



---

### API Group: 03.SingletonKit

#### MonoSingleton<T> (MonoSingleton<T>)

- Type: `QFramework.MonoSingleton`1`
- Namespace: `QFramework`

**Description / 描述**

- 描述: MonoBehaviour 单例类
- Description: MonoBehavior Singleton Class

**Example / 示例**

```csharp
public class GameManager : MonoSingleton<GameManager>
{
    public override void OnSingletonInit()
    {
        Debug.Log(name + ":" + "OnSingletonInit");
    }

    private void Awake()
    {
        Debug.Log(name + ":" + "Awake");
    }

    private void Start()
    {
        Debug.Log(name + ":" + "Start");
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
			
        Debug.Log(name + ":" + "OnDestroy");
    }
}

var gameManager = GameManager.Instance;
// GameManager:OnSingletonInit
// GameManager:Awake
// GameManager:Start
// ---------------------
// GameManager:OnDestroy
```


---

#### Singleton<T> (Singleton<T>)

- Type: `QFramework.Singleton`1`
- Namespace: `QFramework`

**Description / 描述**

- 描述: C# 单例类
- Description: Pure C# Singleton Class

**Example / 示例**

```csharp
public class GameDataManager : Singleton<GameDataManager>
{
    private static int mIndex = 0;

    private Class2Singleton() {}

    public override void OnSingletonInit()
    {
        mIndex++;
    }

    public void Log(string content)
    {
        Debug.Log("GameDataManager" + mIndex + ":" + content);
    }
}

GameDataManager.Instance.Log("Hello");
// GameDataManager1:OnSingletonInit:Hello
GameDataManager.Instance.Log("Hello");
// GameDataManager1:OnSingletonInit:Hello
GameDataManager.Instance.Dispose();
```


---

#### MonoSingletonProperty<T> (MonoSingletonProperty<T>)

- Type: `QFramework.MonoSingletonProperty`1`
- Namespace: `QFramework`

**Description / 描述**

- 描述: 通过属性实现的 MonoSingleton，不占用父类的位置
- Description: MonoSingleton, implemented through property, does not occupy the location of the parent class

**Example / 示例**

```csharp
public class GameManager : MonoBehaviour,ISingleton
{
    public static GameManager Instance
    {
        get { return MonoSingletonProperty<GameManager>.Instance; }
    }
		
    public void Dispose()
    {
    	MonoSingletonProperty<GameManager>.Dispose();
    }
		
    public void OnSingletonInit()
    {
    	Debug.Log(name + ":" + "OnSingletonInit");
    }
    
    private void Awake()
    {
        Debug.Log(name + ":" + "Awake");
    }
    
    private void Start()
    {
        Debug.Log(name + ":" + "Start");
    }
    
    protected void OnDestroy()
    {
        Debug.Log(name + ":" + "OnDestroy");
    }
}
var gameManager = GameManager.Instance;
// GameManager:OnSingletonInit
// GameManager:Awake
// GameManager:Start
// ---------------------
// GameManager:OnDestroy
```


---

#### SingletonProperty<T> (SingletonProperty<T>)

- Type: `QFramework.SingletonProperty`1`
- Namespace: `QFramework`

**Description / 描述**

- 描述: 通过属性实现的 Singleton
- Description: Singleton implemented through properties

**Example / 示例**

```csharp
public class GameDataManager : ISingleton
{
    public static GameDataManager Instance
    {
        get { return SingletonProperty<GameDataManager>.Instance; }
    }

    private GameDataManager() {}
		
    private static int mIndex = 0;

    public void OnSingletonInit()
    {
        mIndex++;
    }

    public void Dispose()
    {
        SingletonProperty<GameDataManager>.Dispose();
    }
		
    public void Log(string content)
    {
        Debug.Log("GameDataManager" + mIndex + ":" + content);
    }
}
 
GameDataManager.Instance.Log("Hello");
// GameDataManager1:OnSingletonInit:Hello
 
GameDataManager.Instance.Log("Hello");
// GameDataManager1:OnSingletonInit:Hello
 
GameDataManager.Instance.Dispose();
```


---

#### MonoSingletonPath (MonoSingletonPathAttribute)

- Type: `QFramework.MonoSingletonPathAttribute`
- Namespace: `QFramework`

**Description / 描述**

- 描述: 修改 MonoSingleton 或者 MonoSingletonProperty 的 gameObject 名字和路径
- Description: Modify the gameObject name and path of the MonoSingleton or MonoSingletonProperty

**Example / 示例**

```csharp
[MonoSingletonPath("[MyGame]/GameManager")]
public class GameManager : MonoSingleton<GameManager>
{
 
}
 
var gameManager = GameManager.Instance;
// ------ Hierarchy ------
// DontDestroyOnLoad
// [MyGame]
//     GameManager
```


---

#### PersistentMonoSingleton<T> (PersistentMonoSingleton<T>)

- Type: `QFramework.PersistentMonoSingleton`1`
- Namespace: `QFramework`

**Description / 描述**

- 描述: 当场景里包含两个 PersistentMonoSingleton，保留先创建的
- Description: when a scenario contains two PersistentMonoSingleton, retain the one that was created first

**Example / 示例**

```csharp
public class GameManager : PersistentMonoSingleton<GameManager>
{
 
}
 
IEnumerator Start()
{
    var gameManager = GameManager.Instance;
 
    var newGameManager = new GameObject().AddComponent<GameManager>();
 
    yield return new WaitForEndOfFrame();
 
    Debug.Log(FindObjectOfTypes<GameManager>().Length);
    // 1
    Debug.Log(gameManager == null);
    // false
    Debug.Log(newGameManager == null);
    // true
}
```


---

#### ReplaceableMonoSingleton<T> (ReplaceableMonoSingleton<T>)

- Type: `QFramework.ReplaceableMonoSingleton`1`
- Namespace: `QFramework`

**Description / 描述**

- 描述: 当场景里包含两个 ReplaceableMonoSingleton，保留最后创建的
- Description: When the scene contains two ReplaceableMonoSingleton, keep the last one created

**Example / 示例**

```csharp
public class GameManager : ReplaceableMonoSingleton<GameManager>
{
 
}

IEnumerator Start()
{
    var gameManager = GameManager.Instance;
 
    var newGameManager = new GameObject().AddComponent<GameManager>();
 
    yield return new WaitForEndOfFrame();
 
    Debug.Log(FindObjectOfTypes<GameManager>().Length);
    // 1
    Debug.Log(gameManager == null);
    // true
    Debug.Log(newGameManager == null);
    // false
}
```


---

### API Group: 04.ActionKit

#### ActionKit (ActionKit)

- Type: `QFramework.ActionKit`
- Namespace: `QFramework`

**Description / 描述**

- 描述: Action 时序动作序列（组合模式 + 命令模式 + 建造者模式）
- Description: Action Sequence (composite pattern + command pattern + builder pattern)

**Properties / 属性**

##### EasyEvent<bool> OnApplicationFocus

- 描述: OnApplicationFocus 生命周期支持
- Description: mono OnApplicationFocus life-circle event support

```csharp
ActionKit.OnApplicationFocus.Register(focus =>
{
    Debug.Log("focus:" + focus);
}).UnRegisterWhenGameObjectDestroyed(gameObject);
```

##### EasyEvent<bool> OnApplicationPause

- 描述: OnApplicationPause 生命周期支持
- Description: mono OnApplicationPause life-circle event support

```csharp
ActionKit.OnApplicationPause.Register(pause =>
            {
                Debug.Log("pause:" + pause);
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
```

##### EasyEvent OnApplicationQuit

- 描述: OnApplicationQuit 生命周期支持
- Description: mono OnApplicationQuit life-circle event support

```csharp
ActionKit.OnApplicationQuit.Register(() =>
{
    Debug.Log("quit");
}).UnRegisterWhenGameObjectDestroyed(gameObject);
```

##### EasyEvent OnFixedUpdate

- 描述: FixedUpdate 生命周期支持
- Description: mono fixed update life-circle event support

```csharp
ActionKit.OnFixedUpdate.Register(() =>
{
    // fixed update code here
    // 这里写 fixed update 相关代码
}).UnRegisterWhenGameObjectDestroyed(gameObject);
```

##### EasyEvent OnGUI

- 描述: OnGUI 生命周期支持
- Description: mono on gui life-circle event support

```csharp
ActionKit.OnGUI.Register(() =>
{
    GUILayout.Label("See Example Code");
    GUILayout.Label("请查看示例代码");
}).UnRegisterWhenGameObjectDestroyed(gameObject);
```

##### EasyEvent OnLateUpdate

- 描述: LateUpdate 生命周期支持
- Description: mono late update life-circle event support

```csharp
ActionKit.OnLateUpdate.Register(() =>
{
    // late update code here
    // 这里写 late update 相关代码
}).UnRegisterWhenGameObjectDestroyed(gameObject);
```

##### EasyEvent OnUpdate

- 描述: Update 生命周期支持
- Description: mono update life-circle event support

```csharp
ActionKit.OnUpdate.Register(() =>
{
    if (Time.frameCount % 30 == 0)
    {
        Debug.Log("Update");
    }
}).UnRegisterWhenGameObjectDestroyed(gameObject);
```


**Methods / 方法**

##### void ComplexAPI()

- 描述: 复合动作示例
- Description: Complex action example

```csharp
ActionKit.Sequence()
        .Callback(() => Debug.Log("Sequence Start"))
        .Callback(() => Debug.Log("Parallel Start"))
        .Parallel(p =>
        {
            p.Delay(1.0f, () => Debug.Log("Delay 1s Finished"))
                .Delay(2.0f, () => Debug.Log("Delay 2s Finished"));
        })
        .Callback(() => Debug.Log("Parallel Finished"))
        .Callback(() => Debug.Log("Check Mouse Clicked"))
        .Sequence(s =>
        {
            s.Condition(() => Input.GetMouseButton(0))
                .Callback(() => Debug.Log("Mouse Clicked"));
        })
        .Start(this, () =>
        {
            Debug.Log("Finish");
        });
// 
// Sequence Start
// Parallel Start
// Delay 1s Finished
// Delay 2s Finished
// Parallel Finished
// Check Mouse Clicked
// ------ After Left Mouse Clicked ------
// ------ 鼠标左键点击后 ------
// Mouse Clicked
// Finish
```

##### IAction Coroutine(Func<IEnumerator> coroutineGetter)

- 描述: 协程支持
- Description: coroutine action example

```csharp
IEnumerator SomeCoroutine()
{
    yield return new WaitForSeconds(1.0f);
    Debug.Log("Hello:" + Time.time);
}
 
ActionKit.Coroutine(SomeCoroutine).Start(this);
// Hello:1.0039           
SomeCoroutine().ToAction().Start(this);
// Hello:1.0039
ActionKit.Sequence()
    .Coroutine(SomeCoroutine)
    .Start(this);
// Hello:1.0039
```

##### IAction Custom(Action<ICustomAPI<object>> customSetting)

- 描述: 自定义动作
- Description: Custom action example

```csharp
ActionKit.Custom(a =>
{
    a
        .OnStart(() => { Debug.Log("OnStart"); })
        .OnExecute(dt =>
        {
            Debug.Log("OnExecute");
 
            a.Finish();
        })
        .OnFinish(() => { Debug.Log("OnFinish"); });
}).Start(this);
             
// OnStart
// OnExecute
// OnFinish
 
class SomeData
{
    public int ExecuteCount = 0;
}
 
ActionKit.Custom<SomeData>(a =>
{
    a
        .OnStart(() =>
        {
            a.Data = new SomeData()
            {
                ExecuteCount = 0
            };
        })
        .OnExecute(dt =>
        {
            Debug.Log(a.Data.ExecuteCount);
            a.Data.ExecuteCount++;
 
            if (a.Data.ExecuteCount >= 5)
            {
                a.Finish();
            }
        }).OnFinish(() => { Debug.Log("Finished"); });
}).Start(this);
         
// 0
// 1
// 2
// 3
// 4
// Finished
 
// 还支持 Sequence、Repeat、Parallel 等
// Also support sequence repeat Parallel
// ActionKit.Sequence()
//     .Custom(c =>
//     {
//         c.OnStart(() => c.Finish());
//     }).Start(this);
```

##### IAction Delay(float seconds, Action callback)

- 描述: 延时回调
- Description: delay callback

```csharp
Debug.Log("Start Time:" + Time.time);
 
ActionKit.Delay(1.0f, () =>
{
    Debug.Log("End Time:" + Time.time);
             
}).Start(this); // update driven
 
// Start Time: 0.000000
---- after 1 seconds ----
---- 一秒后 ----
// End Time: 1.000728
```

##### IAction DelayFrame(int frameCount, Action onDelayFinish)

- 描述: 延时帧
- Description: delay by frameCount

```csharp
Debug.Log("Delay Frame Start FrameCount:" + Time.frameCount);
 
ActionKit.DelayFrame(1, () => { Debug.Log("Delay Frame Finish FrameCount:" + Time.frameCount); })
        .Start(this);
 
ActionKit.Sequence()
        .DelayFrame(10)
        .Callback(() => Debug.Log("Sequence Delay FrameCount:" + Time.frameCount))
        .Start(this);

// Delay Frame Start FrameCount:1
// Delay Frame Finish FrameCount:2
// Sequence Delay FrameCount:11
 
// --- also support nextFrame
// --- 还可以用 NextFrame  
// ActionKit.Sequence()
//      .NextFrame()
//      .Start(this);
//
// ActionKit.NextFrame(() => { }).Start(this);
```

##### IParallel Parallel()

- 描述: 并行动作
- Description: parallel action

```csharp
Debug.Log("Parallel Start:" + Time.time);
 
ActionKit.Parallel()
        .Delay(1.0f, () => { Debug.Log(Time.time); })
        .Delay(2.0f, () => { Debug.Log(Time.time); })
        .Delay(3.0f, () => { Debug.Log(Time.time); })
        .Start(this, () =>
        {
            Debug.Log("Parallel Finish:" + Time.time);
        });
// Parallel Start:0
// 1.01
// 2.01
// 3.02
// Parallel Finish:3.02
```

##### IRepeat Repeat(int repeatCount)

- 描述: 重复动作
- Description: repeat action

```csharp
ActionKit.Repeat()
        .Condition(() => Input.GetMouseButtonDown(0))
        .Callback(() => Debug.Log("Mouse Clicked"))
        .Start(this);
// always Log Mouse Clicked when click left mouse
// 鼠标左键点击时，每次都会输出 Mouse Clicked

ActionKit.Repeat(5) // -1、0 means forever 1 means once  2 means twice
        .Condition(() => Input.GetMouseButtonDown(1))
        .Callback(() => Debug.Log("Mouse right clicked"))
        .Start(this, () =>
        {
            Debug.Log("Right click finished");
        });
// Mouse right clicked
// Mouse right clicked
// Mouse right clicked
// Mouse right clicked
// Mouse right clicked
// Right click finished
```

##### ISequence Sequence()

- 描述: 动作序列
- Description: action sequence

```csharp
Debug.Log("Sequence Start:" + Time.time);
 
ActionKit.Sequence()
    .Callback(() => Debug.Log("Delay Start:" + Time.time))
    .Delay(1.0f)
    .Callback(() => Debug.Log("Delay Finish:" + Time.time))
    .Start(this, _ => { Debug.Log("Sequence Finish:" + Time.time); });
 
// Sequence Start: 0
// Delay Start: 0
------ after 1 seconds ------
------ 1 秒后 ------
// Delay Finish: 1.01012
// Sequence Finish: 1.01012
```

##### IAction Task(Func<Task> taskGetter)

- 描述: Task 支持
- Description: Task action example

```csharp
async Task SomeTask()
{
    await Task.Delay(TimeSpan.FromSeconds(1.0f));
    Debug.Log("Hello:" + Time.time);
}

ActionKit.Task(SomeTask).Start(this);

SomeTask().ToAction().Start(this);

ActionKit.Sequence()
    .Task(SomeTask)
    .Start(this);

// Hello:1.0039
```



---

### API Group: 05.TableKit

#### Table<T> (Table<T>)

- Type: `QFramework.Table`1`
- Namespace: `QFramework`

**Description / 描述**

- 描述: 一类似表格的数据结构，兼顾查询功能和性能，支持联合查询
- Description: A tabular like data structure, both query function and performance, support joint query

**Example / 示例**

```csharp
public class Student
{
    public string Name { get; set; }
    public int Age { get; set; }
    public int Level { get; set; }
}
 
public class School : Table<Student>
{
    public TableIndex<int, Student> AgeIndex = new TableIndex<int, Student>((student) => student.Age);
    public TableIndex<int, Student> LevelIndex = new TableIndex<int, Student>((student) => student.Level);
         
    protected override void OnAdd(Student item)
    {
        AgeIndex.Add(item);
        LevelIndex.Add(item);
    }
 
    protected override void OnRemove(Student item)
    {
        AgeIndex.Remove(item);
        LevelIndex.Remove(item);
    }
 
    protected override void OnClear()
    {
        AgeIndex.Clear();
        LevelIndex.Clear();
    }
 
    public override IEnumerator<Student> GetEnumerator()
    {
        return AgeIndex.Dictionary.Values.SelectMany(s=>s).GetEnumerator();
    }
 
    protected override void OnDispose()
    {
        AgeIndex.Dispose();
        LevelIndex.Dispose();
    }
}
 
 
var school = new School();
school.Add(new Student(){Age = 1,Level = 2,Name = "liangxie"});
school.Add(new Student(){Age = 2,Level = 2,Name = "ava"});
school.Add(new Student(){Age = 3,Level = 2,Name = "abc"});
school.Add(new Student(){Age = 3,Level = 3,Name = "efg"});
            
foreach (var student in school.LevelIndex.Get(2).Where(s=>s.Age < 3))
{
    Debug.Log(student.Age + ":" + student.Level + ":" + student.Name);
}
// 1:2:liangxie
// 2:2:ava
```


---

### API Group: 06.PoolKit

#### SimpleObjectPool<T> (SimpleObjectPool<T>)

- Type: `QFramework.SimpleObjectPool`1`
- Namespace: `QFramework`

**Description / 描述**

- 描述: 面向业务的对象池
- Description: simple object pool

**Example / 示例**

```csharp
class Fish
{
             
}

var pool = new SimpleObjectPool<Fish>(() => new Fish(),initCount:50);
 
Debug.Log(pool.CurCount);
// 50 
var fish = pool.Allocate();
 
Debug.Log(pool.CurCount);
// 49
pool.Recycle(fish);

Debug.Log(pool.CurCount);
// 50


// ---- GameObject ----
var gameObjPool = new SimpleObjectPool<GameObject>(() =>
{
    var gameObj = new GameObject("AGameObject");
    // init gameObj code 

    // gameObjPrefab = Resources.Load<GameObject>("somePath/someGameObj");
                
    return gameObj;
}, (gameObj) =>
{
    // reset code here
});

// ---- Clear ----
gameObjPool.Clear(gameObj=> Object.Destroy(gameObk));
```


---

#### ListPool<T> (ListPool<T>)

- Type: `QFramework.ListPool`1`
- Namespace: `QFramework`

**Description / 描述**

- 描述: 存储 List 对象池，用于优化减少 new 调用次数。
- Description: Store a pool of List objects for optimization to reduce the number of new calls.

**Example / 示例**

```csharp
var names = ListPool<string>.Get()
names.Add("Hello");

names.Release2Pool();
// or ListPool<string>.Release(names);
```


---

#### DictionaryPool<T,K> (DictionaryPool<T,K>)

- Type: `QFramework.DictionaryPool`2`
- Namespace: `QFramework`

**Description / 描述**

- 描述: 存储 Dictionary 对象池，用于优化减少 new 调用次数。
- Description: Store a pool of Dictionary objects for optimization to reduce the number of new calls.

**Example / 示例**

```csharp
var infos = DictionaryPool<string,string>.Get()
infos.Add("name","liangxie");

infos.Release2Pool();
// or DictionaryPool<string,string>.Release(names);
```


---

#### SafeObjectPool<T> (SafeObjectPool<T>)

- Type: `QFramework.SafeObjectPool`1`
- Namespace: `QFramework`

**Description / 描述**

- 描述: 更安全的对象池，带有一定的约束。
- Description: More secure object pooling, with certain constraints.

**Example / 示例**

```csharp
class Bullet :IPoolable,IPoolType
{
    public void OnRecycled()
    {
        Debug.Log("回收了");
    }
 
    public  bool IsRecycled { get; set; }
 
    public static Bullet Allocate()
    {
        return SafeObjectPool<Bullet>.Instance.Allocate();
    }
             
    public void Recycle2Cache()
    {
        SafeObjectPool<Bullet>.Instance.Recycle(this);
    }
}
 
SafeObjectPool<Bullet>.Instance.Init(50,25);
             
var bullet = Bullet.Allocate();
 
Debug.Log(SafeObjectPool<Bullet>.Instance.CurCount);
             
bullet.Recycle2Cache();
 
Debug.Log(SafeObjectPool<Bullet>.Instance.CurCount);
 
// can config object factory
// 可以配置对象工厂
SafeObjectPool<Bullet>.Instance.SetFactoryMethod(() =>
{
    // bullet can be mono behaviour
    return new Bullet();
});
             
SafeObjectPool<Bullet>.Instance.SetObjectFactory(new DefaultObjectFactory<Bullet>());
 
// can set
// 可以设置
// NonPublicObjectFactory: 可以通过调用私有构造来创建对象,can call private constructor to create object
// CustomObjectFactory: 自定义创建对象的方式,can create object by Func<T>
// DefaultObjectFactory: 通过 new 创建对象, can create object by new
```


---

### API Group: 07.ResKit

#### ResKit (ResKit)

- Type: `QFramework.ResKit`
- Namespace: `QFramework`

**Description / 描述**

- 描述: 资源管理方案
- Description: Resource Managements Solution

**Methods / 方法**

##### void Init()

- 描述: 初始化 ResKit
- Description: initialise ResKit

```csharp
ResKit.Init();
```

##### IEnumerator InitAsync()

- 描述: 异步初始化 ResKit，如果是 WebGL 平台，只支持异步初始化
- Description: initialise ResKit async

```csharp
IEnumerator Start()
{
    yield return ResKit.InitAsync();
}

// Or With ActionKit
ResKit.InitAsync().ToAction().Start(this,()=>
{

});
```



---

#### ResLoader Object (ResLoader Object)

- Type: `QFramework.ResLoader`
- Namespace: `QFramework`

**Description / 描述**

- 描述: 资源管理方案
- Description: Resource Managements Solution

**Methods / 方法**

##### ResLoader Allocate()

- 描述: 获取 ResLoader
- Description: Get ResLoader

```csharp
public class MyScript : MonoBehaviour
{
    public ResLoader mResLoader = ResLoader.Allocate();

    ...
}
```

##### void Recycle2Cache()

- 描述: 归还 ResLoader
- Description: Recycle ResLoader

```csharp
public class MyScript : MonoBehaviour
{
    public ResLoader mResLoader = ResLoader.Allocate();

    ...
    void OnDestroy()
    {
        mResLoader.Recycle2Cache();
    }
}
```



---

#### ResLoader API (ResLoader API)

- Type: `QFramework.IResLoaderExtensions`
- Namespace: `QFramework`

**Description / 描述**

- 描述: 资源管理方案
- Description: Resource Managements Solution

**Methods / 方法**

##### void Add2Load(IResLoader self, string assetName, Action<bool, IRes> listener, bool lastOrder)

- 描述: 异步加载资源
- Description: Load Asset Async

```csharp
mResLoader.Add2Load<Texture2D>("MyAsset");
// Or
mResLoader.Add2Load<Texture2D>("MyBundle","MyAsset");

mResLoader.LoadAsync(()=>
{
    // 此时不会触发加载，而是从缓存中获取资源
    // resources are fetched from the cache
    var texture = mResLoader.LoadSync<Texture2D>("MyAsset");
});
```

##### void LoadSceneAsync(IResLoader self, string sceneName, LoadSceneMode loadSceneMode, LocalPhysicsMode physicsMode, Action<AsyncOperation> onStartLoading)

- 描述: 异步加载场景
- Description: Load Scene Sync

```csharp
mResLoader.LoadSceneAsync("BattleScene");
// Or 
mResLoader.LoadSceneAsync("BattleSceneBundle","BattleScene");


mResLoader.LoadSceneAsync("BattleScene",LoadSceneMode.Additive);
//
mResLoader.LoadSceneAsync("BattleScene",LoadSceneMode.Additive,LocalPhysicsMode.Physics2D);


mResLoader.LoadSceneAsync("BattleScene",(operation)=>
{
    Debug.Log(operation.isDone);
});
```

##### void LoadSceneSync(IResLoader self, string assetName, LoadSceneMode mode, LocalPhysicsMode physicsMode)

- 描述: 同步加载场景
- Description: Load Scene Sync

```csharp
mResLoader.LoadSceneSync("BattleScene");
// Or 
mResLoader.LoadSceneSync("BattleSceneBundle","BattleScene");


mResLoader.LoadSceneSync("BattleScene",LoadSceneMode.Additive);
//
mResLoader.LoadSceneSync("BattleScene",LoadSceneMode.Additive,LocalPhysicsMode.Physics2D);
```

##### T LoadSync<T>(IResLoader self, string assetName)

- 描述: 同步加载资源
- Description: Load Asset Sync

```csharp
var texture =mResLoader.LoadSync<Texture2D>("MyAsset");
// Or
texture = mResLoader.LoadSync<Texture2D>("MyBundle","MyAsset");
```



---

### API Group: 08.UIKit

#### UIKit (UIKit)

- Type: `QFramework.UIKit`
- Namespace: `QFramework`

**Description / 描述**

- 描述: 界面管理方案
- Description: UI Managements Solution

**Properties / 属性**

##### UIRoot Root

- 描述: UIKit 界面根节点
- Description: UIKit Root GameObject

```csharp
UIKit.Root.SetResolution(1920,1080,0);
```

##### UIPanelStack Stack

- 描述: UIKit 界面堆栈
- Description: UIKit Panel Stack

```csharp
UIKit.Stack.Push(UIKit.OpenPanel<UIHomePanel>(); // push and close uihomepanel
 
UIKit.Stack.Pop() // pop and open uihomepanel
```


**Methods / 方法**

##### void Back(string currentPanelName)

- 描述: 关闭掉当前界面,返回上一个 Push 过的界面
- Description: Close Current Panel and Back to previous pushed Panel

```csharp
UIKit.Stack.Push(UIKit.OpenPanel<UIHomePanel>());

var basicPanel = UIKit.OpenPanel<UIBasicPanel>();

UIKit.Back(basicPanel);

// UIHomePanel Opened
```

##### void CloseAllPanel()

- 描述: 关闭全部界面
- Description: Close All Panel

```csharp
UIKit.CloseAllPanel();
```

##### void ClosePanel<T>()

- 描述: 关闭界面
- Description: Close Panel

```csharp
UIKit.ClosePanel<UIHomePanel>();

UIKit.ClosePanel("UIHomePanel");
```

##### T GetPanel<T>()

- 描述: 获取界面
- Description: Get Panel

```csharp
var homePanel = UIKit.GetPanel<UIHomePanel>();


UIKit.GetPanel("UIHomePanel");
```

##### void HideAllPanel()

- 描述: 隐藏全部界面
- Description: Hide All Panel

```csharp
UIKit.HideAllPanel();
```

##### void HidePanel<T>()

- 描述: 隐藏界面
- Description: Hide Panel

```csharp
UIKit.HidePanel<UIHomePanel>();

UIKit.HidePanel("UIHomePanel");
```

##### T OpenPanel<T>(PanelOpenType panelOpenType, UILevel canvasLevel, IUIData uiData, string assetBundleName, string prefabName)

- 描述: 打开界面
- Description: Open UI Panel

```csharp
UIKit.OpenPanel<UIHomePanel>();

UIKit.OpenPanel("UIHomePanel");
 
UIKit.OpenPanel<UIHomePanel>(prefabName:"UIHomePanelPrefab");

UIKit.OpenPanel<UIHomePanel>(new UIHomePanelData()
{
    OpenFrom = "GameOverPanel"
});   


UIKit.OpenPanel<UIHomePanel>(UILevel.Common);
```

##### IEnumerator OpenPanelAsync<T>(UILevel canvasLevel, IUIData uiData, string assetBundleName, string prefabName)

- 描述: 异步打开界面
- Description: Open UI Panel Async

```csharp
yield return UIKit.OpenPanelAsync<UIHomePanel>();


// ActionKit Mode
UIKit.OpenPanelAsync<UIHomePanel>().ToAction().Start(this);
```

##### void ShowPanel<T>()

- 描述: 显示界面
- Description: Show Panel

```csharp
UIKit.ShowPanel<UIHomePanel>();

UIKit.ShowPanel("UIHomePanel");
```



---

### API Group: 09.AudioKit

#### AudioKit (AudioKit)

- Type: `QFramework.AudioKit`
- Namespace: `QFramework`

**Description / 描述**

- 描述: 音频管理方案
- Description: Audio Managements Solution

**Properties / 属性**

##### AudioKitSettings Settings

- 描述: 音频相关设置
- Description: AudioKit Setting

```csharp
// Switch
// 开关
btnSoundOn.onClick.AddListener(() => { AudioKit.Settings.IsSoundOn.Value = true; });

btnSoundOff.onClick.AddListener(() => { AudioKit.Settings.IsSoundOn.Value = false; });

btnMusicOn.onClick.AddListener(() => { AudioKit.Settings.IsMusicOn.Value = true; });

btnMusicOff.onClick.AddListener(() => { AudioKit.Settings.IsMusicOn.Value = false; });

btnVoiceOn.onClick.AddListener(() => { AudioKit.Settings.IsVoiceOn.Value = true; });

btnVoiceOff.onClick.AddListener(() => { AudioKit.Settings.IsVoiceOn.Value = false; });

// Volume Control
// 音量控制
AudioKit.Settings.MusicVolume.RegisterWithInitValue(v => musicVolumeSlider.value = v);
AudioKit.Settings.VoiceVolume.RegisterWithInitValue(v => voiceVolumeSlider.value = v);
AudioKit.Settings.SoundVolume.RegisterWithInitValue(v => soundVolumeSlider.value = v);
            
// 监听音量变更
musicVolumeSlider.onValueChanged.AddListener(v => { AudioKit.Settings.MusicVolume.Value = v; });
voiceVolumeSlider.onValueChanged.AddListener(v => { AudioKit.Settings.VoiceVolume.Value = v; });
soundVolumeSlider.onValueChanged.AddListener(v => { AudioKit.Settings.SoundVolume.Value = v; });
```


**Methods / 方法**

##### void PauseMusic()

- 描述: 暂停背景音乐播放
- Description: Pause Background Music

```csharp
AudioKit.PauseMusic();
```

##### void PauseVoice()

- 描述: 暂停人声
- Description: Pause Voice

```csharp
AudioKit.PauseVoice();
```

##### void PlayMusic(string musicName, bool loop, Action onBeganCallback, Action onEndCallback, float volume)

- 描述: 播放背景音乐
- Description: Play Background Music

```csharp
AudioKit.PlayMusic("HomeBg");


// loop = false
AudioKit.PlayMusic("HomeBg",false);

AudioKit.PlayMusic(homeBgClip);
```

##### AudioPlayer PlaySound(string soundName, bool loop, Action<AudioPlayer> callBack, float volume, float pitch)

- 描述: 播放声音
- Description: Play Sound

```csharp
AudioKit.PlaySound("EnemyDie");
AudioKit.PlaySound(EnemyDieClip);
```

##### void PlayVoice(string voiceName, bool loop, Action onBeganCallback, Action onEndedCallback)

- 描述: 播放人声
- Description: Play Voice

```csharp
AudioKit.PlayVoice("SentenceA");
AudioKit.PlayVoice(SentenceAClip);
```

##### void ResumeMusic()

- 描述: 继续背景音乐播放
- Description: Resume Background Music

```csharp
AudioKit.ResumeMusic();
```

##### void ResumeVoice()

- 描述: 继续人声
- Description: Resume Voice

```csharp
AudioKit.ResumeVoice();
```

##### void StopAllSound()

- 描述: 停止播放全部声音
- Description: Stop All Sound

```csharp
AudioKit.StopAllSound();
```

##### void StopMusic()

- 描述: 停止背景音乐播放
- Description: Stop Background Music

```csharp
AudioKit.StopMusic();
```

##### void StopVoice()

- 描述: 停止人声
- Description: Stop Voice

```csharp
AudioKit.StopVoice();
```



---

### API Group: 10.FSM

#### FSM (FSM)

- Type: `QFramework.FSM`1`
- Namespace: `QFramework`

**Description / 描述**

- 描述: 简易状态机
- Description: Simple FSM

**Example / 示例**

```csharp
using UnityEngine;

namespace QFramework.Example
{
    public class IStateBasicUsageExample : MonoBehaviour
    {
        public enum States
        {
            A,
            B
        }

        public FSM<States> FSM = new FSM<States>();

        void Start()
        {
            FSM.OnStateChanged((previousState, nextState) =>
            {
                Debug.Log($"{previousState}=>{nextState}");
            });

            FSM.State(States.A)
                .OnCondition(()=>FSM.CurrentStateId == States.B)
                .OnEnter(() =>
                {
                    Debug.Log("Enter A");
                })
                .OnUpdate(() =>
                {
                    
                })
                .OnFixedUpdate(() =>
                {
                    
                })
                .OnGUI(() =>
                {
                    GUILayout.Label("State A");
                    if (GUILayout.Button("To State B"))
                    {
                        FSM.ChangeState(States.B);
                    }
                })
                .OnExit(() =>
                {
                    Debug.Log("Exit A");
                });

                FSM.State(States.B)
                    .OnCondition(() => FSM.CurrentStateId == States.A)
                    .OnGUI(() =>
                    {
                        GUILayout.Label("State B");
                        if (GUILayout.Button("To State A"))
                        {
                            FSM.ChangeState(States.A);
                        }
                    });
            
                FSM.StartState(States.A);
            }

            private void Update()
            {
                FSM.Update();
            }

            private void FixedUpdate()
            {
                FSM.FixedUpdate();
            }

            private void OnGUI()
            {
                FSM.OnGUI();
            }

            private void OnDestroy()
            {
                FSM.Clear();
            }
        }
    }
}
// Enter A
// Exit A
// A=>B
// Enter B

// class state
using UnityEngine;

namespace QFramework.Example
{
    public class IStateClassExample : MonoBehaviour
    {

        public enum States
        {
            A,
            B,
            C
        }

        public FSM<States> FSM = new FSM<States>();

        public class StateA : AbstractState<States,IStateClassExample>
        {
            public StateA(FSM<States> fsm, IStateClassExample target) : base(fsm, target)
            {
            }

            protected override bool OnCondition()
            {
                return mFSM.CurrentStateId == States.B;
            }

            public override void OnGUI()
            {
                GUILayout.Label("State A");

                if (GUILayout.Button("To State B"))
                {
                    mFSM.ChangeState(States.B);
                }
            }
        }
        
        
        public class StateB: AbstractState<States,IStateClassExample>
        {
            public StateB(FSM<States> fsm, IStateClassExample target) : base(fsm, target)
            {
            }

            protected override bool OnCondition()
            {
                return mFSM.CurrentStateId == States.A;
            }

            public override void OnGUI()
            {
                GUILayout.Label("State B");

                if (GUILayout.Button("To State A"))
                {
                    mFSM.ChangeState(States.A);
                }
            }
        }

        private void Start()
        {
            FSM.AddState(States.A, new StateA(FSM, this));
            FSM.AddState(States.B, new StateB(FSM, this));

            // 支持和链式模式混用
            // FSM.State(States.C)
            //     .OnEnter(() =>
            //     {
            //
            //     });
            
            FSM.StartState(States.A);
        }

        private void OnGUI()
        {
            FSM.OnGUI();
        }

        private void OnDestroy()
        {
            FSM.Clear();
        }
    }
}
```


---

### API Group: 11.GridKit

#### EasyGrid (EasyGrid)

- Type: `QFramework.EasyGrid`1`
- Namespace: `QFramework`

**Description / 描述**

- 描述: Grid 数据结构
- Description: Grid DataStructure

**Example / 示例**

```csharp
using UnityEngine;

namespace QFramework.Example
{
    public class GridKitExample : MonoBehaviour
    {
        // Start is called before the first frame update
        void Start()
        {
            var grid = new EasyGrid<string>(4, 4);

            grid.Fill("Empty");
            
            grid[2, 3] = "Hello";

            grid.Resize(5, 5, (x, y) => "123");

            grid.ForEach((x, y, content) => Debug.Log($"({x},{y}):{content}"));
            
            grid.Clear();
        }
    }
}
(0,0):Empty
(0,1):Empty
(0,2):Empty
(0,3):Empty
(1,0):Empty
(1,1):Empty
(1,2):Empty
(1,3):Empty
(2,0):Empty
(2,1):Empty
(2,2):Empty
(2,3):Hello
(3,0):Empty
(3,1):Empty
(3,2):Empty
(3,3):Empty
(0,0):Empty
(0,1):Empty
(0,2):Empty
(0,3):Empty
(0,4):123
(1,0):Empty
(1,1):Empty
(1,2):Empty
(1,3):Empty
(1,4):123
(2,0):Empty
(2,1):Empty
(2,2):Empty
(2,3):Hello
(2,4):123
(3,0):Empty
(3,1):Empty
(3,2):Empty
(3,3):Empty
(3,4):123
(4,0):123
(4,1):123
(4,2):123
(4,3):123
(4,4):123
```


---

#### DynaGrid (DynaGrid)

- Type: `QFramework.DynaGrid`1`
- Namespace: `QFramework`

**Description / 描述**

- 描述: 动态 Grid 数据结构
- Description: Dynamic Grid DataStructure

**Example / 示例**

```csharp
using UnityEngine;

namespace QFramework.Example
{
    public class DynaGridExample : MonoBehaviour
    {
        public class MyData
        {
            public string Key;
        }

        void Start()
        {
            var dynaGrid = new DynaGrid<MyData>();
            dynaGrid[1, 1] = new MyData() { Key = "Hero" };
            dynaGrid[-1, -10] = new MyData() { Key = "Enemy" };

            dynaGrid.ForEach((x, y, data) => { Debug.Log($"{x} {y} {data.Key}"); });
        }
    }
}

// 1 1 Hero
// -1 -10 Enemy
```

