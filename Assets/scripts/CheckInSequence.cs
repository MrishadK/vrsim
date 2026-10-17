using UnityEngine;
using System.Collections;

public class CheckInSequence : MonoBehaviour
{
    [Header("References")]
    public Transform npc;
    public GameObject bag;
    
    [Header("Conveyor Settings")]
    public Vector3 conveyorDirection = new Vector3(-1, 0, 0); // Direction to slide the bag
    public float conveyorSpeed = 2f;
    public float conveyorDuration = 5f;

    private bool sequenceTriggered = false;

    private void Start()
    {
        if (bag != null)
        {
            bag.SetActive(false); // Hide the bag initially
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (sequenceTriggered) return;

        // Check if player entered
        string n = other.name.ToLower();
        bool isPlayer = other.CompareTag("Player") || other.CompareTag("MainCamera")
                     || n.Contains("player") || n.Contains("xr") 
                     || other.GetComponentInParent<CharacterController>() != null
                     || other.GetComponentInParent<Unity.XR.CoreUtils.XROrigin>() != null
                     || other.GetComponent<Camera>() != null || other.GetComponentInParent<Camera>() != null;

        if (isPlayer)
        {
            sequenceTriggered = true;
            StartCoroutine(SequenceCoroutine(other.transform));
        }
    }

    private IEnumerator SequenceCoroutine(Transform playerObj)
    {
        Debug.Log("Check-In Sequence Started!");

        // 1. NPC Interaction (Pseudo-animation since there is no Animator)
        if (npc != null)
        {
            // Make NPC face the player
            Vector3 lookPos = playerObj.position;
            lookPos.y = npc.position.y;
            npc.rotation = Quaternion.LookRotation(lookPos - npc.position);
            
            // Create a floating speech bubble!
            GameObject speechBubble = new GameObject("SpeechBubble");
            speechBubble.transform.SetParent(npc);
            speechBubble.transform.localPosition = new Vector3(0, 1.8f, 0); // Above head
            
            // Add canvas
            Canvas canvas = speechBubble.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            RectTransform rect = speechBubble.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(2f, 0.5f);
            speechBubble.transform.localScale = new Vector3(1, 1, 1);
            
            // Orient towards player (billboarding roughly)
            speechBubble.transform.rotation = Quaternion.LookRotation(speechBubble.transform.position - playerObj.position);
            
            // Add text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(speechBubble.transform);
            var text = textObj.AddComponent<UnityEngine.UI.Text>();
            text.text = "Hello! Please present your passport and ticket.";
            text.fontSize = 120;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            
            // Outline for readability
            var outline = textObj.AddComponent<UnityEngine.UI.Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(2, -2);
            
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.localPosition = Vector3.zero;
            textRect.localScale = new Vector3(0.01f, 0.01f, 0.01f);
            
            // Simple fast bobbing to simulate talking/gesturing
            Vector3 startPos = npc.position;
            for (int i = 0; i < 20; i++)
            {
                npc.position = startPos + new Vector3(0, Mathf.Sin(Time.time * 20f) * 0.05f, 0);
                yield return new WaitForSeconds(0.1f);
            }
            npc.position = startPos;
            
            // Destroy bubble after a few seconds
            Destroy(speechBubble, 5f);
        }

        // 2. Wait for wordings to be read
        yield return new WaitForSeconds(2.0f);

        // 3. Spawn Bag
        if (bag != null)
        {
            bag.SetActive(true);
            
            // 4. Attach Tag to Bag
            GameObject tagObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tagObj.name = "LuggageTag";
            tagObj.transform.SetParent(bag.transform);
            
            // Bag is scaled to 100, so tag scale must be tiny locally
            tagObj.transform.localScale = new Vector3(0.003f, 0.006f, 0.001f);
            
            // Put tag near the top handle (approximate based on bag bounds)
            tagObj.transform.localPosition = new Vector3(0, 0.008f, 0.002f);
            tagObj.transform.localRotation = Quaternion.Euler(45, 0, 0);

            // Give it a bright green color
            var mr = tagObj.GetComponent<MeshRenderer>();
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = Color.green;
            mr.sharedMaterial = mat;

            yield return new WaitForSeconds(1f);

            // 5. Move along conveyor
            float t = 0;
            while (t < conveyorDuration)
            {
                t += Time.deltaTime;
                bag.transform.position += conveyorDirection * conveyorSpeed * Time.deltaTime;
                yield return null;
            }
        }
    }
}
