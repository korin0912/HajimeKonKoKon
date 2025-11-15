using UnityEngine;

public class Block : MonoBehaviour
{
    public enum Type
    {
        コ,
        ン,
    }

    [SerializeField] private Type type;
    public Type BlockType => type;

    [SerializeField] private GameObject eraseEffectPrefab;

    public bool isErased = false;

    public void SetPosition(RectTransform gridTransform)
    {
        (transform as RectTransform).position = gridTransform.position;
    }

    public void Erase()
    {
        if (isErased)
        {
            return;
        }

        isErased = true;

        if (eraseEffectPrefab)
        {
            var effGo = Instantiate(eraseEffectPrefab);
            effGo.transform.position = transform.position;
        }

        Destroy(gameObject);
    }
}
