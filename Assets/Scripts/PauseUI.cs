using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class PauseUI : MonoBehaviour
{
    public GameObject pauseMenuRoot;
    public GameObject mainMenu;

    public List<GameObject> menuList;
    public List<Button> menuButtons;

    private bool isPaused = false;
    private int currentMenu = 0;

    private void Start()
    {
        for(int i = 0; i < menuButtons.Count; i++)
        {
            
            int menu = i + 1; //Funny bug here, if you just use ChangeMenu(i+1) inside of the delegate, it will add one twice :)
            menuButtons[i].onClick.AddListener(delegate { ChangeMenu(menu);}); //This is why I love c#, can actually have one function for menu changes instead of an individual one for each of them.
        }
        {
            //i.onClick.AddListener(onButtonPressed);
        }
        pauseMenuRoot.SetActive(isPaused);
    }

    void ChangeMenu(int newMenu)
    {
        if(newMenu == 0)
        {
            menuList[currentMenu - 1].SetActive(false);
            mainMenu.SetActive(true);
        } 
        else
        {
            menuList[newMenu - 1].SetActive(true);
            mainMenu.SetActive(false);
        }
        currentMenu = newMenu;
    }

    void Update()
    {
        if (Input.GetKeyUp(KeyCode.Escape))
        {
            if (!isPaused)
            {
                isPaused = true;
            }
            else
            {
                if(currentMenu == 0)
                {
                    isPaused = false;
                } else
                {
                    ChangeMenu(0);
                }
            }
            pauseMenuRoot.SetActive(isPaused);
        }


    }
}
