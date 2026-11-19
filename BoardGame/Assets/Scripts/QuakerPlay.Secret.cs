using System.Collections;
using UnityEngine;

// The secret card (digital only): Wrecking Ball. When an event flip turns up the secret card (QuakerBalance:
// secretCardOn, secretCardChance), a wrecking ball drops onto the tower and knocks every block off the tower base.
// The tower has collapsed, so the game ends the usual way (R-16): whoever placed the most recent block loses.
// Blocks knocked off by the ball are never strikes (R-27 is only about placing).
public partial class QuakerPlay
{
    IEnumerator WreckingBall()
    {
        busy = true;
        Add("  SECRET CARD: a wrecking ball drops on the tower.");
        QueueBanner("Secret card: Wrecking Ball!", "A wrecking ball drops on the site. The whole tower comes down.", new Color(1f, 0.35f, 0.25f));
        yield return new WaitForSeconds(1.2f);   // let everyone read the card

        // The ball: big and heavy, dropped from high above the centre of the tower base.
        float top = baseTop;
        foreach (Rigidbody rb in placed) if (rb != null) top = Mathf.Max(top, RenderBounds(rb.gameObject).max.y);
        GameObject ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ball.name = "Wrecking Ball";
        float size = cell * 2.2f;
        ball.transform.localScale = Vector3.one * size;
        ball.transform.position = baseCentre + Vector3.up * (top - baseTop + size + 30f);
        Renderer br = ball.GetComponent<Renderer>();
        if (br != null)
        {
            Material m = br.material;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", new Color(0.16f, 0.16f, 0.18f));
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0.8f);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.6f);
        }
        Rigidbody ballBody = ball.AddComponent<Rigidbody>();
        ballBody.mass = blockMass * 80f;
        ballBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        ballBody.linearVelocity = Vector3.down * 25f;

        // Wait for it to reach the tower (or the base, if the tower is low).
        float t0 = Time.time;
        while (Time.time - t0 < 3f && ball.transform.position.y - size * 0.5f > top + 0.5f) yield return null;
        GameSounds.Play(GameSound.Collapse);

        // The impact: everything still on the tower base is thrown outward, until nothing is left on it.
        for (int pass = 0; pass < 4; pass++)
        {
            bool any = false;
            foreach (Rigidbody rb in placed)
            {
                if (rb == null || !OnTower(rb)) continue;
                any = true;
                Vector3 away = rb.worldCenterOfMass - baseCentre;
                away.y = 0f;
                if (away.sqrMagnitude < 0.01f) away = Random.insideUnitCircle.normalized;   // a block right in the middle
                Vector3 push = away.normalized * 14f + Vector3.up * 6f + Random.insideUnitSphere * 2f;
                rb.AddForce(push, ForceMode.VelocityChange);
                rb.AddTorque(Random.insideUnitSphere * 8f, ForceMode.VelocityChange);
            }
            if (!any) break;
            yield return new WaitForSeconds(1.2f);
        }
        Destroy(ball, 1.5f);

        // The usual check: every block is off the base, so the tower has collapsed (R-16).
        yield return SettleThenCheck(false);
    }
}
