using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour {
    [SerializeField] private Image portraitImage;
    [SerializeField] private Text nameText;
    [SerializeField] private Text dialogueText;
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private float typeSpeed = 0.03f;

    [Header("Audio")]
    [SerializeField] private AudioSource voiceAudioSource; 

    [Header("Choices")]
    [SerializeField] private GameObject choicesScrollViewHolder; 
    [SerializeField] private Transform choicesContentContainer; 
    [SerializeField] private GameObject choiceButtonPrefab; 

    private List<DialogueLine> lines; // Работаем со старой структурой внутри
    private List<DialogueChoice> currentChoices; 

    private int currentLine = 0;
    private bool isDialogueActive = false;
    private bool waitingForChoice = false; 

    private Coroutine typingCoroutine;
    private bool isTyping = false;
    private string currentFullText;

    private FP_Controller playerController;
    private FP_CameraLook playerCamera;

    void Start() {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) {
            playerController = player.GetComponent<FP_Controller>();
            playerCamera = player.GetComponent<FP_CameraLook>();
        }

        if (voiceAudioSource == null) voiceAudioSource = GetComponent<AudioSource>();
        
        ClearChoices();
    }


    // --- СТАРЫЙ МЕТОД (для обратной совместимости) ---
    public void StartDialogue(DialogueAsset dialogueAsset)
    {
        if (dialogueAsset == null) return;
        InitializeDialogue(dialogueAsset.lines, dialogueAsset.choices);
    }
    // Специальный метод для UI кнопки (Android/Mouse)
// Эту функцию вешаем на Button -> OnClick()
public void OnNextLineButtonPressed()
{
    // Если диалог не идет или мы ждем выбора — не тыкаем
    if (!isDialogueActive || waitingForChoice) return;

    // САМОЕ ВАЖНОЕ: Проверка "Печатаем или нет?"
    if (isTyping)
    {
        // ВАРИАНТ 1: Текст еще печатается
        // Останавливаем печатную машинку
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        
        // Показываем весь текст сразу
        dialogueText.text = currentFullText;
        
        // Говорим системе, что мы закончили печатать
        isTyping = false;
    }
    else
    {
        // ВАРИАНТ 2: Текст уже полностью на экране
        // Значит, переходим к следующей фразе
        ShowNextLine();
    }
}
    // --- НОВЫЙ МЕТОД (для Advanced ассетов) ---
    public void StartDialogue(AdvancedDialogueAsset advancedAsset) {
        if (advancedAsset == null) return;

        // Конвертируем "Реплики" в плоский список "Линий" на лету
        List<DialogueLine> flattenedLines = new List<DialogueLine>();

        if (advancedAsset.replicas != null) {
            foreach (var replica in advancedAsset.replicas) {
                // Если забыли назначить персонажа, пропускаем или ставим заглушку
                if (replica.characterProfile == null) continue;

                foreach (var phrase in replica.phrases) {
                    DialogueLine newLine = new DialogueLine();
                    
                    // Данные из профиля
                    newLine.characterName = replica.characterProfile.characterName;
                    newLine.characterIcon = replica.characterProfile.defaultIcon;

                    // Данные из фразы (оверрайды)
                    newLine.text = phrase.text;
                    newLine.voiceClip = phrase.voiceClip;
                    
                    if (phrase.iconOverride != null) {
                        newLine.characterIcon = phrase.iconOverride;
                    }

                    flattenedLines.Add(newLine);
                }
            }
        }

        InitializeDialogue(flattenedLines, advancedAsset.choices);
    }

    // Общая логика инициализации
    private void InitializeDialogue(List<DialogueLine> loadedLines, List<DialogueChoice> loadedChoices) {
        if (!isDialogueActive) {
            dialoguePanel.SetActive(true);
            isDialogueActive = true;
        }

        lines = loadedLines;
        currentChoices = loadedChoices;
        
        currentLine = 0;
        waitingForChoice = false;
        ClearChoices();

        if (lines == null || lines.Count == 0) {
            CheckForChoicesOrEnd();
        } else {
            ShowLine();
        }
    }

    public void ShowNextLine() {
        if (currentLine < lines.Count - 1) {
            currentLine++;
            ShowLine();
        } else {
            CheckForChoicesOrEnd();
        }
    }

    private void CheckForChoicesOrEnd() {
        if (currentChoices != null && currentChoices.Count > 0) {
            ShowChoices();
        } else {
            EndDialogue();
        }
    }

    private void ShowChoices() {
        waitingForChoice = true; 
        dialogueText.text = ""; 
        
        if (choicesScrollViewHolder != null) 
            choicesScrollViewHolder.SetActive(true);

        foreach (var choice in currentChoices) {
            GameObject btnObj = Instantiate(choiceButtonPrefab, choicesContentContainer);
            Button btn = btnObj.GetComponent<Button>();
            
            Text btnText = btnObj.GetComponentInChildren<Text>(); 
            if (btnText != null) {
                btnText.text = LanguageManager.GetText(choice.buttonText);
            }

            // --- ЛОГИКА ВЫБОРА ---
            // Запоминаем ссылки для замыкания (lambda)
            AdvancedDialogueAsset nextAdv = choice.nextAdvancedDialogue;
            DialogueAsset nextLegacy = choice.nextDialogue;

            btn.onClick.AddListener(() => {
                ClearChoices(); // Сначала чистим кнопки
                
                if (nextAdv != null) {
                    StartDialogue(nextAdv); // Приоритет новому типу
                } 
                else if (nextLegacy != null) {
                    StartDialogue(nextLegacy); // Если нового нет, ищем старый
                } 
                else {
                    EndDialogue(); // Если ничего нет — выход
                }
            });
        }
    }
    
    // Метод OnChoiceSelected можно удалить, так как мы перенесли логику внутрь лямбды выше.
    
    // ПРИМЕЧАНИЕ: Если ты хочешь переходить на Advanced диалоги через кнопки,
    // тебе нужно изменить класс DialogueChoice, добавив туда поле public AdvancedDialogueAsset nextAdvancedDialogue;
    // и здесь проверять: если nextAdvanced != null -> StartDialogue(nextAdvanced)
    
    private void OnChoiceSelected(DialogueAsset nextDialogue) {
        ClearChoices();
        if (nextDialogue != null) {
            StartDialogue(nextDialogue);
        } else {
            EndDialogue();
        }
    }

    private void ClearChoices() {
        if (choicesContentContainer != null) {
            foreach (Transform child in choicesContentContainer) {
                Destroy(child.gameObject);
            }
        }
        if (choicesScrollViewHolder != null) {
            choicesScrollViewHolder.SetActive(false);
        }
    }

    void ShowLine() {
        if (lines == null || lines.Count <= currentLine) return;

        DialogueLine line = lines[currentLine];
        nameText.text = line.characterName;
        portraitImage.sprite = line.characterIcon;

        if (voiceAudioSource != null) {
            voiceAudioSource.Stop(); 
            if (line.voiceClip != null) {
                voiceAudioSource.clip = line.voiceClip;
                voiceAudioSource.Play();
            }
        }

        currentFullText = LanguageManager.GetText(line.text);

        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeText(currentFullText));
    }

    IEnumerator TypeText(string fullText) {
        isTyping = true;
        dialogueText.text = "";
        foreach (char c in fullText.ToCharArray()) {
            dialogueText.text += c;
            yield return new WaitForSeconds(typeSpeed);
        }
        isTyping = false;
    }

    void EndDialogue() {
        if (voiceAudioSource != null) voiceAudioSource.Stop();
        dialoguePanel.SetActive(false);
        isDialogueActive = false;
        waitingForChoice = false;
        ClearChoices();

        if (playerController == null || playerCamera == null) {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) {
                playerController = player.GetComponent<FP_Controller>();
                playerCamera = player.GetComponent<FP_CameraLook>();
            }
        }
        if (playerController != null) playerController.canControl = true;
        if (playerCamera != null) playerCamera.StopLook();
    }
}