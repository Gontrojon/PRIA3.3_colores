using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace HelloWorld
{
    public class HelloWorldPlayer : NetworkBehaviour
    {
        public NetworkVariable<Vector3> Position = new NetworkVariable<Vector3>();
        // variable network para almacenar el int correspondiente a la posicion de la lista del color
        public NetworkVariable<Color> PlayerColorNumber;

        // lista con los materiales de los colores que puede ser el player
        public List<Color> playerColors;
        // lista que se usara para clonar y saber que colores disponibles hay
        private List<Color> disponibles;
        // renderrer para poder cambiar el color al player
        private Renderer rend;

        private void Start()
        {
            // se guarda la referencia al renderer para no buscarla todo el rato
            rend = GetComponent<Renderer>();
        }

        public override void OnNetworkSpawn()
        {
            // se subscribe a los cambios realizados en la networkvariable
            PlayerColorNumber.OnValueChanged += OnPlayerColorNumberChanged;
            rend = GetComponent<Renderer>();
            // si se es propietario se asegura de generar un color valido y ponerselo
            if (IsOwner)
            {
                Move();
                CambiarMaterial();
            }
            // si no se es propietario se asegura de que el resto de jugadores tengan sus colores sincronizados
            if (!IsOwner)
            {
                // si no es el propietario y hay mas jugadores se le asigna el color que tenga
                rend.material.color = PlayerColorNumber.Value;
            }
        }

        public override void OnNetworkDespawn()
        {
            // Se elimina la suscripcion cuando se desconecta
            PlayerColorNumber.OnValueChanged -= OnPlayerColorNumberChanged;
        }

        public void CambiarMaterial()
        {
            if (NetworkManager.Singleton.IsServer)
            {
                // se pide si el color es valido
                PlayerColorNumber.Value = ColorValido();
                // se asigna el color
                rend.material.color = PlayerColorNumber.Value;
            }
            else
            {
                // peticion al server para el cambio de color
                SubmitColorRequestServerRpc();
            }
        }

        public void Move()
        {
            if (NetworkManager.Singleton.IsServer)
            {
                var randomPosition = GetRandomPositionOnPlane();
                transform.position = randomPosition;
                Position.Value = randomPosition;

            }
            else
            {
                SubmitPositionRequestServerRpc();
            }
        }
        
        public void OnPlayerColorNumberChanged(Color previous, Color current)
        {
            Debug.Log("Se detecto un cambio en la networkvariable");
            // cuando se detecta un cambio en la networkvariable se asigna el nuevo valor
            rend.material.color = PlayerColorNumber.Value;
        }

        [ServerRpc]
        void SubmitPositionRequestServerRpc(ServerRpcParams rpcParams = default)
        {
            Position.Value = GetRandomPositionOnPlane();
        }

        [ServerRpc]
        void SubmitColorRequestServerRpc(ServerRpcParams rpcParams = default)
        {
            // se guarda el color valido
            PlayerColorNumber.Value = ColorValido();
        }
        
        static Vector3 GetRandomPositionOnPlane()
        {
            return new Vector3(Random.Range(-5f, 5f), 1f, Random.Range(-3f, 3f));
        }

        Color ColorValido()
        {
            // lista para guardar los colores de los clientes conectados
            List<Color> coloresUsados = new List<Color>();
            // se recorre los clientes conectados para guardar los colores que estan usando
            foreach (ulong uid in NetworkManager.Singleton.ConnectedClientsIds)
            {
                // se añade a la lista
                coloresUsados.Add(NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(uid).GetComponent<HelloWorldPlayer>().PlayerColorNumber.Value);
            }
            // se hace un clon de la lista completa de colores
            disponibles = new List<Color>( playerColors);
            // Se elimina los colores que se han usado
            disponibles.RemoveAll(coloresUsados.Contains);
            // si la lista esta vacia porque no quedan colores se asgina siempre negro
            if (disponibles.Count == 0)
            {
                return Color.black;
            }
            // se devuelve un color aleatorio de los disponibles
            return disponibles[Random.Range(0, disponibles.Count)];
        }


        void Update()
        {
            transform.position = Position.Value;
            /*
            // comprobacion para que no asigne el color en cada frame, sino solo cuando el valor es diferente
            if (rend.material != playerMaterial[PlayerColorNumber.Value])
            {
                // se asigna el nuevo material
                rend.material = playerMaterial[PlayerColorNumber.Value];
            }
            */
        }
    }
}
