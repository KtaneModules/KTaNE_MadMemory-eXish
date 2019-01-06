using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MadMemory : MonoBehaviour
{
    public KMSelectable[] buttons;
    public TextMesh screenText;
    public MeshRenderer[] leds;
    public string[] screenTexts;
    public KMSelectable submitButton;

    public Material off;
    public Material green;
    public Material yellow;
    public Material red;
	
    public int[] buttonNumbers = new int[4];
    public bool[,] buttonStates = new bool[4, 4];
    public string[,] buttonLabels = new string[4, 4];
    public int[] screenLabels = new int[4];
	
	int stage = 0;
    int correctIndex;
    bool isActivated = true;
	
    private static int _moduleIdCounter = 1;
    private int _moduleId = 0;

    void Start()
    {

        _moduleId = _moduleIdCounter++;
        GetComponent<KMBombModule>().OnActivate += ActivateModule;
    }

    void Init()
    {
		RandomizeButtons(0);
        for(int i = 0; i < buttons.Length; i++)
        {
            leds[i].material = off;
            int j = i;
            buttons[i].OnInteract += delegate () { OnPress(j); return false; };
        }
        submitButton.OnInteract += delegate () { OnSubmit(); return false; };
    }
	
	void RandomizeButtons(int currentStage)
	{
		buttonNumbers = new[] {0, 0, 0, 0};
        buttonNumbers[0] = UnityEngine.Random.Range(1, 5);
        buttonNumbers[1] = UnityEngine.Random.Range(1, 5);
        do
        {
            buttonNumbers[1] = UnityEngine.Random.Range(1, 5);
        } while (buttonNumbers[1] == buttonNumbers[0]);
        buttonNumbers[2] = UnityEngine.Random.Range(1, 5);
        do
        {
            buttonNumbers[2] = UnityEngine.Random.Range(1, 5);
        } while (buttonNumbers[2] == buttonNumbers[0] || buttonNumbers[2] == buttonNumbers[1]);
        buttonNumbers[3] = UnityEngine.Random.Range(1, 5);
        do
        {
            buttonNumbers[3] = UnityEngine.Random.Range(1, 5);
        } while (buttonNumbers[3] == buttonNumbers[0] || buttonNumbers[3] == buttonNumbers[1] || buttonNumbers[3] == buttonNumbers[2]);

        for(int i = 0; i < buttons.Length; i++)
        {
			buttonStates[currentStage, i] = false;
            string label = buttonNumbers[i].ToString();
			buttonLabels[currentStage, i] = label;
            TextMesh buttonText = buttons[i].GetComponentInChildren<TextMesh>();
            buttonText.text = label;
        }
		screenLabels[currentStage] = UnityEngine.Random.Range(0, 16);
        screenText.text = screenTexts[screenLabels[currentStage]];
		
	}

    void ActivateModule()
    {
        Init();
		
        isActivated = true;
		bool[] result = CorrectResult();
		string correct = "";
		for (int i = 0; i < 4; i++)
		{
			if (result[i])
			{
				correct = correct + buttonLabels[0, i];
			}
		}
		Debug.LogFormat("[Mad Memory #{0}] Expecting button group \"{1}\".", _moduleId, correct == "" ? "(none)" : correct);
    }

    void OnPress(int pressedButton)
    {
        GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        GetComponent<KMSelectable>().AddInteractionPunch();

        if (!isActivated)
        {
			Debug.LogFormat("[Mad Memory #{0}] Pressed a button before the module has been activated. Resetting the module.", _moduleId);
            GetComponent<KMBombModule>().HandleStrike();
			isActivated = false;
            StartCoroutine(RedLights());
			RandomizeButtons(0);
        }
        else
        {
			buttonStates[stage, pressedButton] = !buttonStates[stage, pressedButton];
			if (buttonStates[stage, pressedButton])
			{
				//Debug.LogFormat("[Mad Memory #{0}] Selected button labeled \"{1}\".", _moduleId, buttonLabels[stage, pressedButton]);
				leds[pressedButton].material = yellow;
			}
			else if (pressedButton < stage)
			{
				//Debug.LogFormat("[Mad Memory #{0}] Deselected button labeled \"{1}\".", _moduleId, buttonLabels[stage, pressedButton]);
				leds[pressedButton].material = green;
			}
			else
			{
				//Debug.LogFormat("[Mad Memory #{0}] Deselected button labeled \"{1}\".", _moduleId, buttonLabels[stage, pressedButton]);
				leds[pressedButton].material = off;
			}
        }
    }
	
    void OnSubmit()
    {
        GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        GetComponent<KMSelectable>().AddInteractionPunch();
		bool[] result = CorrectResult(false);
		string sequence = "";
		for (int i = 0; i < 4; i++)
		{
			if (buttonStates[stage, i])
			{
				sequence = sequence + buttonLabels[stage, i];
			}
		}
		sequence = (sequence == "") ? "(none)" : "\"" + sequence + "\"";
		string correct = "";
		for (int i = 0; i < 4; i++)
		{
			if (result[i])
			{
				correct = correct + buttonLabels[stage, i];
			}
		}
		correct = (correct == "") ? "(none)" : "\"" + correct + "\"";
		Debug.LogFormat("[Mad Memory #{0}] Received button group {1}.", _moduleId, sequence);
		if (buttonStates[stage, 0] == result[0] && buttonStates[stage, 1] == result[1] && buttonStates[stage, 2] == result[2] && buttonStates[stage, 3] == result[3])
		{
			isActivated = false;
			if (stage < 3)
			{
				Debug.LogFormat("[Mad Memory #{0}] Button group {1} was correct. Advancing to stage {2}.", _moduleId, sequence, stage + 2);
				RandomizeButtons(stage + 1);
			}
			else
			{
				screenText.text = "";
				Debug.LogFormat("[Mad Memory #{0}] Module solved!", _moduleId);
				GetComponent<KMBombModule>().HandlePass();
				GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
			}
			StartCoroutine(GreenLights());
        }
		else
		{
			Debug.LogFormat("[Mad Memory #{0}] Button group {1} was incorrect (expected {2}). Resetting the module.", _moduleId, sequence, correct);
			GetComponent<KMBombModule>().HandleStrike();
			isActivated = false;
            StartCoroutine(RedLights());
			RandomizeButtons(0);
		}
    }
	
	bool[] CorrectResult(bool log = true)
	{
	bool[] result = new[] {false, false, false, false};
		if (log)
		{
			Debug.LogFormat("[Mad Memory #{0}] Stage {1}. Screen label is: {2}. Button labels are: {3}.", _moduleId, stage + 1, screenTexts[screenLabels[stage]], buttonLabels[stage, 0] + buttonLabels[stage, 1] + buttonLabels[stage, 2] + buttonLabels[stage, 3]);
		}
		switch (stage)
		{
			//STAGE 1
			
			case 0:
				//The type is:
				switch (screenLabels[0] / 4)
				{
					//Digit: 3rd position
					case 0:
						if (log)
						{
							Debug.LogFormat("[Mad Memory #{0}] Label is a digit. Rule 1: 3rd position.", _moduleId);
						}
						result[2] = true;
						break;
						
					//Two digits: 2nd position
					case 1:
						if (log)
						{
							Debug.LogFormat("[Mad Memory #{0}] Label is a two-digit number. Rule 1: 2nd position.", _moduleId);
						}
						result[1] = true;
						break;
						
					//Numeral: label "2"
					case 2:
						if (log)
						{
							Debug.LogFormat("[Mad Memory #{0}] Label is a numeral. Rule 1: label \"2\".", _moduleId);
						}
						for (int i = 0; i < 4; i++)
						{
							if (buttonLabels[0, i] == "2")
							{
								result[i] = true;
								break;
							}
						}
						break;
						
					//Word: label "1"
					case 3:
						if (log)
						{
							Debug.LogFormat("[Mad Memory #{0}] Label is a word. Rule 1: label \"1\".", _moduleId);
						}
						for (int i = 0; i < 4; i++)
						{
							if (buttonLabels[0, i] == "1")
							{
								result[i] = true;
								break;
							}
						}
						break;
				}
				
				//The value is:
				switch (screenLabels[0] % 4)
				{
					//1: 4th position
					case 0:
						if (log)
						{
							Debug.LogFormat("[Mad Memory #{0}] Label's value is 1. Rule 2: 4th position.", _moduleId);
						}
						result[3] = true;
						break;
						
					//2: label "3"
					case 1:
						if (log)
						{
							Debug.LogFormat("[Mad Memory #{0}] Label's value is 2. Rule 2: label \"3\".", _moduleId);
						}
						for (int i = 0; i < 4; i++)
						{
							if (buttonLabels[0, i] == "3")
							{
								result[i] = true;
								break;
							}
						}
						break;
						
					//3: label "4"
					case 2:
						if (log)
						{
							Debug.LogFormat("[Mad Memory #{0}] Label's value is 3. Rule 2: label \"4\".", _moduleId);
						}
						for (int i = 0; i < 4; i++)
						{
							if (buttonLabels[0, i] == "4")
							{
								result[i] = true;
								break;
							}
						}
						break;
						
					//4: 1st position
					case 3:
						if (log)
						{
							Debug.LogFormat("[Mad Memory #{0}] Label's value is 4. Rule 2: 1st position.", _moduleId);
						}
						result[0] = true;
						break;
				}
				return result;
				
			//STAGE 2
			
			case 1:
			
				//same screen as in stage 1: all positions not from stage 1
				if (screenLabels[1] == screenLabels[0])
				{
					if (log)
					{
						Debug.LogFormat("[Mad Memory #{0}] Label is the same as in stage 1. Rule: all positions left unselected in stage 1.", _moduleId);
					}
					for (int i = 0; i < 4; i++)
					{
						if (!buttonStates[0, i])
						{
							result[i] = true;
						}
					}
				}
				
				//same type as in stage 1: all labels from stage 1
				else if (screenLabels[1] / 4 == screenLabels[0] / 4)
				{
					if (log)
					{
						Debug.LogFormat("[Mad Memory #{0}] Label is the same type as in stage 1. Rule: all labels selected in stage 1.", _moduleId);
					}
					for (int i = 0; i < 4; i++)
					{
						for (int j = 0; j < 4; j++)
						{
							if (buttonLabels[1, i] == buttonLabels[0, j] && buttonStates[0, j])
							{
								result[i] = true;
								break;
							}
						}
					}
				}
				
				//same value as in stage 1: all labels less or equal to the number of characters on stage 1's display
				else if (screenLabels[1] % 4 == screenLabels[0] % 4)
				{
					if (log)
					{
						Debug.LogFormat("[Mad Memory #{0}] Label has the same value as in stage 1. Rule: all labels that are less or equal to the number of characters on the display in stage 1.", _moduleId);
					}
					for (int i = 0; i < 4; i++)
					{
						if (buttonLabels[1, i][0] - '0' <= screenTexts[screenLabels[0]].Length)
						{
							result[i] = true;
						}
					}
				}
				
				//otherwise: label "4"; position #(displayed value)
				else
				{
					if (log)
					{
						Debug.LogFormat("[Mad Memory #{0}] None of the conditions apply. Rule: label \"4\" and position equal to the displayed value.", _moduleId);
					}
					for (int i = 0; i < 4; i++)
					{
						if (buttonLabels[1, i] == "4")
						{
							result[i] = true;
							break;
						}
					}
					result[screenLabels[1] % 4] = true;
				}
				
				return result;
				
			//STAGE 2
			
			case 2:
			
				//same screen as in stage 1 or 2: all labels not from stage 2
				if (screenLabels[2] == screenLabels[0] || screenLabels[2] == screenLabels[1])
				{
					if (log)
					{
						Debug.LogFormat("[Mad Memory #{0}] Label is the same as in stage 1 or 2. Rule: all labels left unselected in stage 2.", _moduleId);
					}
					for (int i = 0; i < 4; i++)
					{
						for (int j = 0; j < 4; j++)
						{
							if (buttonLabels[2, i] == buttonLabels[1, j] && !buttonStates[1, j])
							{
								result[i] = true;
								break;
							}
						}
					}
				}
				
				//same type as in stage 2: all labels not from stage 1
				else if (screenLabels[2] / 4 == screenLabels[1] / 4)
				{
					if (log)
					{
						Debug.LogFormat("[Mad Memory #{0}] Label is the same type as in stage 2. Rule: all labels left unselected in stage 1.", _moduleId);
					}
					for (int i = 0; i < 4; i++)
					{
						for (int j = 0; j < 4; j++)
						{
							if (buttonLabels[2, i] == buttonLabels[0, j] && !buttonStates[0, j])
							{
								result[i] = true;
								break;
							}
						}
					}
				}
				
				//same value as in stage 1: label (displayed value); amount of characters on stage 2's display
				else if (screenLabels[2] % 4 == screenLabels[0] % 4)
				{
					if (log)
					{
						Debug.LogFormat("[Mad Memory #{0}] Label has the same value as in stage 1. Rule: label equal to the displayed value and label that is equal to the number of characters on the display in stage 2.", _moduleId);
					}
					for (int i = 0; i < 4; i++)
					{
						if (buttonLabels[2, i][0] - '0' == screenLabels[2] % 4 + 1 || buttonLabels[2, i][0] - '0' == screenTexts[screenLabels[1]].Length)
						{
							result[i] = true;
						}
					}
				}
				
				//otherwise: all unselected labels and all unselected positions
				else
				{
					if (log)
					{
						Debug.LogFormat("[Mad Memory #{0}] None of the conditions apply. Rule: all previously unselected labels and all previously unselected positions.", _moduleId);
					}
					for (int i = 0; i < 4; i++)
					{
						result[i] = true;
						for (int j = 0; j < 4; j++)
						{
							if (buttonLabels[2, i] == buttonLabels[0, j] && buttonStates[0, j] || buttonLabels[2, i] == buttonLabels[1, j] && buttonStates[1, j])
							{
								result[i] = false;
								break;
							}
						}
					}
					for (int i = 0; i < 4; i++)
					{
						if (!buttonStates[0, i] && !buttonStates[1, i])
						{
							result[i] = true;
						}
					}
				}
				
				return result;
			
			//STAGE 4
			
			case 3:
			
				//3 different values in stages 1-3: positions #(respective values)
				if (screenLabels[2] % 4 != screenLabels[0] % 4 && screenLabels[2] % 4 != screenLabels[1] % 4 && screenLabels[1] % 4 != screenLabels[0] % 4)
				{
					if (log)
					{
						Debug.LogFormat("[Mad Memory #{0}] Stages 1-3 all had different values. Rule: positions equal to those values.", _moduleId);
					}
					for (int i = 0; i < 3; i++)
					{
						result[screenLabels[i] % 4] = true;
					}
				}
				
				else
				{
					//checking how many selected labels
					int selectedLabelAmount = 0;
					for (int i = 0; i < 4; i++)
					{
						result[i] = true;
						for (int j = 0; j < 4; j++)
						{
							if (buttonLabels[3, i] == buttonLabels[0, j] && buttonStates[0, j] || buttonLabels[3, i] == buttonLabels[1, j] && buttonStates[1, j] || buttonLabels[3, i] == buttonLabels[2, j] && buttonStates[2, j])
							{
								selectedLabelAmount++;
								result[i] = false;
								break;
							}
						}
					}
					
					//exactly 3 selected labels: every positions except #(unselected label)
					if (selectedLabelAmount == 3)
					{
						if (log)
						{
							Debug.LogFormat("[Mad Memory #{0}] Exactly 1 unselected label. Rule: positions not equal to that label.", _moduleId);
						}
						for (int i = 0; i < 4; i++)
						{
							if (result[i])
							{
								for (int j = 0; j < 4; j++)
								{
									result[j] = true;
								}
								result[buttonLabels[3, i][0] - '0' - 1] = false;
								break;
							}
						}
					}
					
					//same type as in any previous stages: labels (values displayed on any such stages)
					else if (screenLabels[3] / 4 == screenLabels[2] / 4 || screenLabels[3] / 4 == screenLabels[1] / 4 || screenLabels[3] / 4 == screenLabels[0] / 4)
					{
						if (log)
						{
							Debug.LogFormat("[Mad Memory #{0}] Label is the same type as in any of the previous stages. Rule: labels equal to the values displayed on any such stages.", _moduleId);
						}
						for (int i = 0; i < 4; i++)
						{
							result[i] = false;
						}
						for (int i = 0; i < 3; i++)
						{
							if (screenLabels[3] / 4 == screenLabels[i] / 4)
							{
								for (int j = 0; j < 4; j++)
								{
									if (buttonLabels[3, j][0] - '0' == screenLabels[i] % 4 + 1)
									{
										result[j] = true;
										break;
									}
								}
							}
						}
					}
					
					//same value as in any previous stages: position #(value displayed on all such stages)
					else if (screenLabels[3] % 4 == screenLabels[2] % 4 || screenLabels[3] % 4 == screenLabels[1] % 4 || screenLabels[3] % 4 == screenLabels[0] % 4)
					{
						if (log)
						{
							Debug.LogFormat("[Mad Memory #{0}] Label has the same value as in any of the previous stages. Rule: position equal to the value displayed on all such stages.", _moduleId);
						}
						for (int i = 0; i < 4; i++)
						{
							result[i] = false;
						}
						result[screenLabels[3] % 4] = true;
					}
					
					//otherwise: every position selected less than 3 times total
					else
					{
						if (log)
						{
							Debug.LogFormat("[Mad Memory #{0}] None of the conditions apply. Rule: every position selected less than 3 times total.", _moduleId);
						}
						for (int i = 0; i < 4; i++)
						{
							result[i] = true;
							if (buttonStates[0, i] && buttonStates[1, i] && buttonStates[2, i])
							{
								result[i] = false;
							}
						}
					}
				}
				
				return result;
			
		}
		if (log)
		{
			Debug.LogFormat("[Mad Memory #{0}] Warning: Correct buttons not computed. Returning empty group.", _moduleId);
		}
		return new[] {false, false, false, false};
	}
	
    IEnumerator RedLights()
    {
        for (int i = 0; i < 4; i++)
        {
            leds[i].material = red;
        }
        yield return new WaitForSeconds(0.1f);
        for (int i = 0; i < 4; i++)
        {
            leds[i].material = off;
        }
        yield return new WaitForSeconds(0.1f);
        for (int i = 0; i < 4; i++)
        {
            leds[i].material = red;
        }
        yield return new WaitForSeconds(0.1f);
        for (int i = 0; i < 4; i++)
        {
            leds[i].material = off;
        }
        yield return new WaitForSeconds(0.1f);
        for (int i = 0; i < 4; i++)
        {
            leds[i].material = red;
        }
        yield return new WaitForSeconds(0.6f);
        for (int i = 0; i < 4; i++)
        {
            leds[i].material = off;
        }
		stage = 0;
		ActivateModule();
    }
	
    IEnumerator GreenLights()
    {
        for (int i = 0; i < 4; i++)
        {
            leds[i].material = green;
        }
        yield return new WaitForSeconds(0.1f);
        for (int i = 0; i < 4; i++)
        {
            leds[i].material = off;
        }
        yield return new WaitForSeconds(0.1f);
        for (int i = 0; i < 4; i++)
        {
            leds[i].material = green;
        }
        yield return new WaitForSeconds(0.1f);
        for (int i = 0; i < 4; i++)
        {
            leds[i].material = off;
        }
        yield return new WaitForSeconds(0.1f);
        for (int i = 0; i < 4; i++)
        {
            leds[i].material = green;
        }
        yield return new WaitForSeconds(0.6f);
		stage++;
		if (stage < 4)
		{
			bool[] result = CorrectResult();
			string correct = "";
			for (int i = 0; i < 4; i++)
			{
			if (result[i])
				{
					correct = correct + buttonLabels[stage, i];
				}
			}
			Debug.LogFormat("[Mad Memory #{0}] Expecting button group \"{1}\".", _moduleId, correct == "" ? "(none)" : correct, stage + 1);
		}
		for(int i = 0; i < leds.Length; i++)
		{
			if (stage < 4)
			{
				buttonStates[stage, i] = false;
			}
			if (i < stage)
			{
				leds[i].material = green;
			}
			else
			{
				leds[i].material = off;
			}
		}
        isActivated = true;
    }
}

