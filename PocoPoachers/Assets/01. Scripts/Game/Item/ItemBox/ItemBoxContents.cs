using UnityEngine;

// 공통 부모에 제한을 두어 랜덤/고정 파생 컴포넌트도 한 박스에 하나만 허용한다.
[DisallowMultipleComponent]
[RequireComponent(typeof(ItemBox))]
public abstract class ItemBoxContents : MonoBehaviour
{
}
