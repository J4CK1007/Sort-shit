using UnityEngine;

public class NextItemInput : MonoBehaviour
{
    [SerializeField] private ItemSequencePresenter presenter;

    private void Update()
    {
        if (!presenter) return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            presenter.PresentNext();
        }
    }
}
