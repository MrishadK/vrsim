using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Flight Information Display System (FIDS) for modern airport departures/arrivals.
/// Realistically cycles and updates flight schedules in real-time.
/// </summary>
public class FIDSDisplay : MonoBehaviour
{
    [System.Serializable]
    public struct FlightEntry
    {
        public string flightNo;
        public string destination;
        public string time;
        public string gate;
        public string status;
        public Color statusColor;
    }

    [Header("Display Text Reference")]
    [SerializeField] private TextMeshProUGUI boardText;
    [SerializeField] private float updateInterval = 4f;

    private readonly List<FlightEntry> baseSchedule = new List<FlightEntry>()
    {
        new FlightEntry { flightNo = "EK 202", destination = "DUBAI (DXB)", time = "14:20", gate = "A01", status = "BOARDING", statusColor = new Color(0.2f, 1f, 0.4f) },
        new FlightEntry { flightNo = "BA 117", destination = "LONDON (LHR)", time = "14:35", gate = "A04", status = "GATE OPEN", statusColor = new Color(0.2f, 0.8f, 1f) },
        new FlightEntry { flightNo = "LH 440", destination = "FRANKFURT (FRA)", time = "14:50", gate = "B02", status = "ON TIME", statusColor = Color.white },
        new FlightEntry { flightNo = "AF 334", destination = "PARIS (CDG)", time = "15:05", gate = "B06", status = "ON TIME", statusColor = Color.white },
        new FlightEntry { flightNo = "SQ 026", destination = "SINGAPORE (SIN)", time = "15:15", gate = "A08", status = "DELAYED 15M", statusColor = new Color(1f, 0.8f, 0.2f) },
        new FlightEntry { flightNo = "QR 701", destination = "DOHA (DOH)", time = "15:30", gate = "C03", status = "ON TIME", statusColor = Color.white },
        new FlightEntry { flightNo = "JL 006", destination = "TOKYO (HND)", time = "15:45", gate = "A12", status = "ON TIME", statusColor = Color.white },
        new FlightEntry { flightNo = "AA 100", destination = "NEW YORK (JFK)", time = "16:00", gate = "B10", status = "CHECK-IN", statusColor = new Color(0.4f, 0.7f, 1f) }
    };

    private void Start()
    {
        if (boardText == null)
        {
            boardText = GetComponentInChildren<TextMeshProUGUI>();
        }

        StartCoroutine(FIDSUpdateRoutine());
    }

    private IEnumerator FIDSUpdateRoutine()
    {
        while (true)
        {
            RenderBoard();
            yield return new WaitForSeconds(updateInterval);
        }
    }

    private void RenderBoard()
    {
        if (boardText == null) return;

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("<b><color=#00E5FF>TIME   FLIGHT   DESTINATION             GATE   STATUS</color></b>");
        sb.AppendLine("<color=#557799>────────────────────────────────────────────────────────────</color>");

        for (int i = 0; i < baseSchedule.Count; i++)
        {
            var f = baseSchedule[i];
            string hex = ColorUtility.ToHtmlStringRGB(f.statusColor);
            sb.AppendLine($"<color=#E0E8F0>{f.time,-6}</color> <color=#FFD700>{f.flightNo,-8}</color> <color=#FFFFFF>{f.destination,-23}</color> <color=#00FFCC>{f.gate,-6}</color> <color=#{hex}>{f.status}</color>");
        }

        boardText.text = sb.ToString();
    }
}
