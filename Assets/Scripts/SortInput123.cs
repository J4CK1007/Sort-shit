using UnityEngine;
using UnityEngine;

public class SortInput123 : MonoBehaviour
{
    [SerializeField] private ItemSequencePresenter presenter;

    private void Update()
    {
        if (!presenter) return;
        if (presenter.CurrentItem == null) return;

        // Player A ¡ª ASD -> piles 1/2/3
        if (Input.GetKeyDown(KeyCode.A))
        {
            presenter.SortCurrentIntoPile(1);
        }
        else if (Input.GetKeyDown(KeyCode.S))
        {
            presenter.SortCurrentIntoPile(2);
        }
        else if (Input.GetKeyDown(KeyCode.D))
        {
            presenter.SortCurrentIntoPile(3);
        }
        // Player B ¡ª JKL -> piles 4/5/6
        else if (Input.GetKeyDown(KeyCode.J))
        {
            presenter.SortCurrentIntoPile(4);
        }
        else if (Input.GetKeyDown(KeyCode.K))
        {
            presenter.SortCurrentIntoPile(5);
        }
        else if (Input.GetKeyDown(KeyCode.L))
        {
            presenter.SortCurrentIntoPile(6);
        }
    }
}
