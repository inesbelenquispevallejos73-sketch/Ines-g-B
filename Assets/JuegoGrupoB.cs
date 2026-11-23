using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// JUEGO GRUPO B - Se crea solo al dar Play
// P = saltar | SPACE = avanzar | ↑ = retroceder
// Personaje: usa Assets/Resources/lisa.png si existe; si no, un personaje pixel art.
public class JuegoGrupoB : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Iniciar()
    {
        if (FindAnyObjectByType<JuegoGrupoB>() == null)
            new GameObject("Jugador").AddComponent<JuegoGrupoB>();
    }

    public float velocidad = 6f;
    public float fuerzaSalto = 15f;
    public float limiteCaidaY = -8f;

    string[] comentarios = {
        "¡Te caíste!", "¡Ups! Al vacío...", "¡Cuidado con el hueco!",
        "¡Casi lo logras!", "¡Inténtalo de nuevo!"
    };

    Rigidbody2D rb;
    SpriteRenderer srJugador;
    AudioSource audioSrc;
    AudioClip beep;
    Sprite cuadro;
    Vector3 inicio;
    bool enSuelo;
    int caidas;
    float tiempoMensaje;
    string mensaje = "";

    void Start()
    {
        // 1. Ocultar objetos viejos de la escena
        foreach (var t in FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            string n = t.name.ToLower();
            if (n.StartsWith("suelo") || n.StartsWith("personaje")) t.gameObject.SetActive(false);
        }

        // 2. Cuadrado base para las plataformas
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        cuadro = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        beep = CrearBeep();

        // 3. Escenario
        Color blanco = Color.white;
        CrearBloque("Suelo A", new Vector2(-6.2f, -3.5f), new Vector2(9.1f, 1f), blanco);
        CrearBloque("Suelo B", new Vector2(6.1f, -3.5f), new Vector2(9.2f, 1f), blanco);
        CrearBloque("Bloque 1", new Vector2(-4.7f, -0.4f), new Vector2(4.5f, 1.3f), blanco);
        CrearBloque("Bloque 2", new Vector2(2.3f, -0.65f), new Vector2(4.5f, 1.3f), blanco);
        CrearBloque("Bloque 3", new Vector2(8.2f, 2.05f), new Vector2(4.5f, 1.3f), blanco);

        // 4. Jugador
        transform.position = new Vector3(-9.5f, 0.7f, 0f);
        inicio = transform.position;
        srJugador = gameObject.AddComponent<SpriteRenderer>();
        srJugador.sortingOrder = 1;

        Sprite imagen = Resources.Load<Sprite>("lisa");   // tu imagen (opcional)
        if (imagen != null)
        {
            srJugador.sprite = imagen;
            float s = 1.5f / imagen.bounds.size.y;           // altura = 1.5
            transform.localScale = new Vector3(s, s, 1f);
        }
        else
        {
            srJugador.sprite = CrearPersonajePixel();        // personaje original
        }

        var col = gameObject.AddComponent<BoxCollider2D>();
        col.sharedMaterial = new PhysicsMaterial2D { friction = 0f };
        rb = gameObject.AddComponent<Rigidbody2D>();
        rb.gravityScale = 3f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        audioSrc = gameObject.AddComponent<AudioSource>();

        // 5. Cámara fija con fondo azul
        Camera cam = Camera.main;
        if (cam == null)
        {
            var c = new GameObject("Camara");
            c.tag = "MainCamera";
            cam = c.AddComponent<Camera>();
            c.AddComponent<AudioListener>();
        }
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.transform.position = new Vector3(0f, 0f, -10f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.19f, 0.29f, 0.47f);
    }

    void Update()
    {
        bool avanzar = false, retroceder = false, saltar = false;
#if ENABLE_INPUT_SYSTEM
        var k = Keyboard.current;
        if (k != null)
        {
            avanzar = k.spaceKey.isPressed;
            retroceder = k.upArrowKey.isPressed;
            saltar = k.pKey.wasPressedThisFrame;
        }
#else
        avanzar = Input.GetKey(KeyCode.Space);
        retroceder = Input.GetKey(KeyCode.UpArrow);
        saltar = Input.GetKeyDown(KeyCode.P);
#endif
        float x = 0f;
        if (avanzar) x = 1f;
        if (retroceder) x = -1f;
        rb.linearVelocity = new Vector2(x * velocidad, rb.linearVelocity.y);

        // Mirar hacia donde camina
        if (x < 0) srJugador.flipX = true;
        if (x > 0) srJugador.flipX = false;

        if (saltar && enSuelo)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, fuerzaSalto);

        // Cada vez que se cae: comentario + vuelve al inicio
        if (transform.position.y < limiteCaidaY)
        {
            caidas++;
            mensaje = comentarios[Random.Range(0, comentarios.Length)];
            tiempoMensaje = 2.5f;
            transform.position = inicio;
            rb.linearVelocity = Vector2.zero;
        }
        if (tiempoMensaje > 0f) tiempoMensaje -= Time.deltaTime;
    }

    void FixedUpdate() { enSuelo = false; }

    void OnCollisionStay2D(Collision2D c)
    {
        foreach (var p in c.contacts)
            if (p.normal.y > 0.5f) enSuelo = true;
    }

    // Sonido al golpear un bloque con la cabeza
    void OnCollisionEnter2D(Collision2D c)
    {
        if (!c.gameObject.name.StartsWith("Bloque")) return;
        foreach (var p in c.contacts)
            if (p.normal.y < -0.5f) { audioSrc.PlayOneShot(beep); break; }
    }

    void OnGUI()
    {
        var chico = new GUIStyle(GUI.skin.label) { fontSize = 20 };
        chico.normal.textColor = Color.white;
        GUI.Label(new Rect(10, 10, 600, 30), "P: saltar | SPACE: avanzar | ↑: atrás | Caídas: " + caidas, chico);

        if (tiempoMensaje <= 0f) return;
        var grande = new GUIStyle(GUI.skin.label)
        {
            fontSize = 50,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        grande.normal.textColor = Color.red;
        GUI.Label(new Rect(0, 0, Screen.width, Screen.height), mensaje, grande);
    }

    // Personaje original en pixel art (8x12)
    Sprite CrearPersonajePixel()
    {
        string[] dibujo = {
            "..HHHH..",
            ".HHHHHH.",
            ".HSSSSH.",
            ".SESSES.",
            ".SSSSSS.",
            "..SSSS..",
            "..BBBB..",
            ".BBBBBB.",
            "SBBBBBBS",
            "..PPPP..",
            "..P..P..",
            ".KK..KK."
        };
        int w = 8, h = dibujo.Length;
        var tex = new Texture2D(w, h);
        tex.filterMode = FilterMode.Point;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                Color c;
                switch (dibujo[y][x])
                {
                    case 'H': c = new Color(0.45f, 0.25f, 0.1f); break;  // pelo café
                    case 'S': c = new Color(1f, 0.8f, 0.65f); break;     // piel
                    case 'E': c = Color.black; break;                    // ojos
                    case 'B': c = new Color(0.2f, 0.75f, 0.4f); break;   // polera verde
                    case 'P': c = new Color(0.25f, 0.25f, 0.3f); break;  // pantalón
                    case 'K': c = new Color(0.9f, 0.3f, 0.2f); break;    // zapatos
                    default: c = Color.clear; break;
                }
                tex.SetPixel(x, h - 1 - y, c);
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 8f);
    }

    GameObject CrearBloque(string nombre, Vector2 pos, Vector2 tam, Color color)
    {
        var g = new GameObject(nombre);
        g.transform.position = pos;
        g.transform.localScale = new Vector3(tam.x, tam.y, 1f);
        var sr = g.AddComponent<SpriteRenderer>();
        sr.sprite = cuadro;
        sr.color = color;
        g.AddComponent<BoxCollider2D>();
        return g;
    }

    AudioClip CrearBeep()
    {
        int hz = 44100, n = hz / 6;
        float[] datos = new float[n];
        for (int i = 0; i < n; i++)
            datos[i] = Mathf.Sin(2f * Mathf.PI * 880f * i / hz) * (1f - (float)i / n) * 0.5f;
        var clip = AudioClip.Create("beep", n, 1, hz, false);
        clip.SetData(datos, 0);
        return clip;
    }
}