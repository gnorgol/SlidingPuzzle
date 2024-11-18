using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;


public class GameManager : MonoBehaviour
{
    [SerializeField] private Transform gameTransform;
    public Button importButton;
    public Button startButton;
    public Button defaultImageButton;
    private int size;
    private List<Transform> pieces;
    public MeshRenderer originalImage;
    private Material originalMaterial;
    private int emptyLocation;
    private bool isShuffling = false;
    public GameObject puzzleDisplay;
    [DllImport("__Internal")]
    
    private static extern void UploadFile();

    private void Start()
    {
        importButton.onClick.AddListener(OnImportButtonClick);

        startButton.onClick.AddListener(StartPuzzle);
        startButton.interactable = false;

        defaultImageButton.onClick.AddListener(OnDefaultImageButtonClick); // Ajout de l'écouteur d'événements
        puzzleDisplay.SetActive(false);
    }

    public void StartPuzzle()
    {

        ResetGamePieces();
        pieces = new List<Transform>();
        size = 4;
        CreateGamePieces(0.01f);

        puzzleDisplay.SetActive(true);
        RawImage displayImage = puzzleDisplay.GetComponent<RawImage>();
        displayImage.texture = originalMaterial.mainTexture;

    }

    private void ResetGamePieces()
    {
        if (pieces == null) { return; }
        foreach (Transform piece in pieces)
        {
            Destroy(piece.gameObject);
        }
        pieces.Clear();
    }

    void Update()
    {
        // Check for completion.
        if (!isShuffling && CheckCompletion())
        {
            isShuffling = true;
            StartCoroutine(WaitShuffle(0.5f));
        }

        // On click send out ray to see if we click a piece.
        if (Input.GetMouseButtonDown(0) && !isShuffling)
        {
            RaycastHit2D hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
            if (hit)
            {
                // Go through the list, the index tells us the position.
                for (int i = 0; i < pieces.Count; i++)
                {
                    if (pieces[i] == hit.transform)
                    {
                        // Check each direction to see if valid move.
                        // We break out on success so we don't carry on and swap back again.
                        if (TrySwap(i, -size)) { break; }
                        if (TrySwap(i, +size)) { break; }
                        if (TrySwap(i, -1)) { break; }
                        if (TrySwap(i, +1)) { break; }
                    }
                }
            }
        }
    }
    private void OnDefaultImageButtonClick()
    {
        Sprite sprite = Resources.Load<Sprite>("image"); // Assurez-vous que l'image est dans un dossier Resources
        Debug.Log(sprite);

        // Charger l'image par défaut
        Texture2D defaultImage = SpriteToTexture2D(sprite);
        originalMaterial = CreateMaterial(defaultImage);
        if (defaultImage != null)
        {
            if (originalImage != null)
            {
                MeshRenderer meshRenderer = originalImage.gameObject.GetComponent<MeshRenderer>();
                if (meshRenderer != null)
                {
                    meshRenderer.material = originalMaterial;
                    startButton.interactable = true;
                }
                else
                {
                    Debug.LogError("MeshRenderer introuvable sur l'objet originalImage !");
                }
            }
            else
            {
                Debug.LogError("originalImage n'est pas assigné !");
            }
        }
        else
        {
            Debug.LogError("Image par défaut introuvable !");
        }
    }
    private void OnImportButtonClick()
    {
        UploadFile();  // Appel au JavaScript pour ouvrir le sélecteur de fichiers
    }
    public void OnFileUploaded(string imageData)
    {
        try
        {
            byte[] imageBytes = System.Convert.FromBase64String(imageData);
            Texture2D ImportImage = new Texture2D(2, 2);
            ImportImage.LoadImage(imageBytes);
            originalMaterial = CreateMaterial(ImportImage);
            if (originalImage != null)
            {
                MeshRenderer meshRenderer = originalImage.gameObject.GetComponent<MeshRenderer>();
                if (meshRenderer != null)
                {
                    meshRenderer.material = originalMaterial;
                    startButton.interactable = true;
                }
                else
                {
                    Debug.LogError("MeshRenderer introuvable sur l'objet originalImage !");
                }
            }
            else
            {
                Debug.LogError("originalImage n'est pas assigné !");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("Erreur lors du téléchargement du fichier : " + ex.Message);
        }
    }
    private bool CheckCompletion()
    {
        if (pieces != null)
        {
            for (int i = 0; i < pieces.Count; i++)
            {
                if (pieces[i].name != $"{i}")
                {
                    return false;
                }
            }
        }
        return true;
    }

    private Texture2D SpriteToTexture2D(Sprite sprite)
    {
        if (sprite.rect.width != sprite.texture.width)
        {
            Texture2D newText = new Texture2D((int)sprite.rect.width, (int)sprite.rect.height);
            Color[] newColors = sprite.texture.GetPixels((int)sprite.textureRect.x,
                                                         (int)sprite.textureRect.y,
                                                         (int)sprite.textureRect.width,
                                                         (int)sprite.textureRect.height);
            newText.SetPixels(newColors);
            newText.Apply();
            return newText;
        }
        else
            return sprite.texture;
    }
    private Material CreateMaterial(Texture2D texture)
    {
        Material material = new Material(Shader.Find("Unlit/Texture"));
        if (material == null)
        {
            Debug.LogError("Shader 'Unlit/Texture' not found. Make sure it is included in the build.");
        }
        material.mainTexture = texture;
        return material;
    }
    private void CreateGamePieces(float gapThickness)
    {
        float width = 1f / size;
        float gap = gapThickness / 2f;

        for (int row = 0; row < size; row++)
        {
            for (int col = 0; col < size; col++)
            {
                Transform piece = Instantiate(originalImage.transform, gameTransform);
                pieces.Add(piece);

                piece.localPosition = new Vector3(
                    -1 + (2 * width * col) + width,
                    1 - (2 * width * row) - width,
                    0
                );
                piece.localScale = ((2 * width) - gapThickness) * Vector3.one;
                piece.name = $"{(row * size) + col}";

                if (row == size - 1 && col == size - 1)
                {
                    emptyLocation = (size * size) - 1;
                    piece.gameObject.SetActive(false);
                }
                else
                {
                    Mesh mesh = piece.GetComponent<MeshFilter>().mesh;
                    Vector2[] uv = new Vector2[4]
                    {
                        new Vector2((width * col) + gap, 1 - ((width * (row + 1)) - gap)),
                        new Vector2((width * (col + 1)) - gap, 1 - ((width * (row + 1)) - gap)),
                        new Vector2((width * col) + gap, 1 - ((width * row) + gap)),
                        new Vector2((width * (col + 1)) - gap, 1 - ((width * row) + gap))
                    };
                    mesh.uv = uv;
                }
            }
        }
    }
    private IEnumerator WaitShuffle(float duration)
    {
        yield return new WaitForSeconds(duration);
        Shuffle();
        isShuffling = false;
    }
    private void Shuffle()
    {
        int count = 0;
        int last = -1; // Initialize to -1 to ensure the first move is always valid
        while (count < (size * size * size))
        {
            // Pick a random location.
            int rnd = UnityEngine.Random.Range(0, size * size);
            // Only thing we forbid is undoing the last move.
            if (rnd == last) { continue; }
            last = rnd;
            // Try surrounding spaces looking for valid move.
            if (TrySwap(rnd, -size) || TrySwap(rnd, +size) || TrySwap(rnd, -1) || TrySwap(rnd, +1))
            {
                count++;
            }
        }
    }

    private bool TrySwap(int index, int offset)
    {
        int targetIndex = index + offset;
        if (IsValidSwap(index, targetIndex))
        {
            SwapPieces(index, targetIndex);
            return true;
        }
        return false;
    }

    private bool IsValidSwap(int index, int targetIndex)
    {
        if (targetIndex < 0 || targetIndex >= size * size) return false;
        if (index % size == 0 && targetIndex == index - 1) return false; // Prevent wrapping from left to right
        if (index % size == size - 1 && targetIndex == index + 1) return false; // Prevent wrapping from right to left
        return targetIndex == emptyLocation;
    }

    private void SwapPieces(int index, int targetIndex)
    {
        // Swap them in game state.
        (pieces[index], pieces[targetIndex]) = (pieces[targetIndex], pieces[index]);
        // Swap their transforms.
        (pieces[index].localPosition, pieces[targetIndex].localPosition) = (pieces[targetIndex].localPosition, pieces[index].localPosition);
        // Update empty location.
        emptyLocation = index;
    }


}
