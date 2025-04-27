using UnityEngine;
using UnityEngine.Windows;

public class InputManager : MonoBehaviour
{
    private PlayerInput _playerInput;
    public PlayerInput.PlayerActions InGame { get; set; }

    public void Init()
    {
        _playerInput = new();
        _playerInput.Player.Enable();
        _playerInput.Player.Attack.performed += ctx => GameManager.Instance.player.Attack();
        _playerInput.Player.Dash.performed += ctx =>
        {
            GameManager.Instance.player.Dash();
        };
    }

    private void Update()
    {
        if (GameManager.Instance.player == null)
            return;
        GameManager.Instance.player.OnMove(_playerInput.Player.Move.ReadValue<Vector2>());
    }
}
