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
        public NetworkVariable<int> PlayerColorNumber;

        // lista con los materiales de los colores que puede ser el player
        public List<Material> playerMaterial;
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
                rend.material = playerMaterial[PlayerColorNumber.Value];
            }
        }

        public override void OnNetworkDespawn()
        {
            PlayerColorNumber.OnValueChanged -= OnPlayerColorNumberChanged;
        }

        public void CambiarMaterial()
        {
            int number;
            if (NetworkManager.Singleton.IsServer)
            {
                // se pide si el color es valido
                number = NumeroValido();
                // se asigna el color
                rend.material = playerMaterial[number];
                // se guarda el numero a la posicion del color de la lista
                PlayerColorNumber.Value = number;
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
        
        public void OnPlayerColorNumberChanged(int previous, int current)
        {
            Debug.Log("Se detecto un cambio en la networkvariable");
            rend.material = playerMaterial[PlayerColorNumber.Value];
        }

        [ServerRpc]
        void SubmitPositionRequestServerRpc(ServerRpcParams rpcParams = default)
        {
            Position.Value = GetRandomPositionOnPlane();
        }

        [ServerRpc]
        void SubmitColorRequestServerRpc(ServerRpcParams rpcParams = default)
        {
            // se pide numero valido
            int number = NumeroValido();
            // se guarda el numero valido
            PlayerColorNumber.Value = number;
        }
        
        [ServerRpc(RequireOwnership = false)]
        public void ToggleServerRpc(int n)
        {
            // this will cause a replication over the network
            // and ultimately invoke `OnValueChanged` on receivers
            Debug.Log("Se llevara a cabo la replicacion de red");
            PlayerColorNumber.Value =   n;
        }

        static Vector3 GetRandomPositionOnPlane()
        {
            return new Vector3(Random.Range(-3f, 3f), 1f, Random.Range(-3f, 3f));
        }

        int NumeroValido()
        {
            // se crea la variable que almacenara el numero valido
            int number;
            // variable usada en el do-while
            bool iguales;
            // lista para guardar los numeros de los clientes conectados
            List<int> numerosUsados = new List<int>();
            // se recorre los clientes conectados para guardar los numeros que estan usando
            foreach (ulong uid in NetworkManager.Singleton.ConnectedClientsIds)
            {
                // se a�ade a la lista
                numerosUsados.Add(NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(uid).GetComponent<HelloWorldPlayer>().PlayerColorNumber.Value);
            }
            // do while para que solo se pueda obtener numeros validos
            do
            {
                // se genera numero 
                number = Random.Range(0, playerMaterial.Count);
                // se comprueba que no estea en la lista
                iguales = numerosUsados.Contains(number);
            } while (iguales);
            // se devuelve el numero valido
            return number;
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