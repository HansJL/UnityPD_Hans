using UnityEngine;

public class SimpleLfoAnim : MonoBehaviour
{ void Start()
{
    
}

void Update()
{
    float value = GlobalLfos.Instance.Get("slowSine");
    transform.localPosition = new Vector3(0, value, 0);
}
}