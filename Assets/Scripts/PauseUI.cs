using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using UnityEngine;
using System.Runtime.InteropServices;
using UnityEngine.UI;

public class PauseUI : MonoBehaviour
{
    public FreeCam freeCamController;

    public GameObject pauseMenuRoot;
    public GameObject mainMenu;

    public List<GameObject> menuList;
    public List<Button> menuButtons;
    public List<int> subMenuOf;


    private bool isPaused = false;
    private int currentMenu = 0;
    private bool moveWasEnabled = false;


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
        if (newMenu == 0)
        {
            menuList[currentMenu - 1].SetActive(false);
            mainMenu.SetActive(true);
        }
        else
        {
            if (currentMenu == 0)
            {
                mainMenu.SetActive(false);
            } 
            else
            {
                menuList[currentMenu - 1].SetActive(false);
            }
            menuList[newMenu - 1].SetActive(true);
            
        }
        currentMenu = newMenu;
    }

    void Update()
    {
        if (Input.GetKeyUp(KeyCode.Escape))
        {
            if (!isPaused)
            {
                SetPaused(true);
            }
            else
            {
                if(currentMenu == 0)
                {
                    SetPaused(false);
                } else
                {
                    ChangeMenu(subMenuOf[currentMenu-1]);
                }
            }
            pauseMenuRoot.SetActive(isPaused);
        }


    }

    void SetPaused(bool paused)
    {
        if (isPaused && (!paused))
        {
            if (moveWasEnabled)
            {
                freeCamController.SetMoveEnabled(true);
                moveWasEnabled = false;
            }
            isPaused = false;
            pauseMenuRoot.SetActive(false);
        }
        else if(paused && (!isPaused))
        {
            if (freeCamController.GetMoveEnabled())
            {
                moveWasEnabled = true;
                freeCamController.SetMoveEnabled(false);
            }
            isPaused = true;
            pauseMenuRoot.SetActive(true);
        }
    }
}
