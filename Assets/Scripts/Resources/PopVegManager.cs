﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.IO;
using System.Globalization;

public class PopVegManager : MonoBehaviour
{
    // Variabels for updating UI
    public Slider popSlider;
    
    // Within that radius, there are some injuries.
    public int effectRadius;

    // Within that radius, everyone dies.
    public int exterminationRadius;

    public GameObject gameOverScreen;

    [HideInInspector]
    // pop_table is a tab with dimensions 180 x 360
    // pop_table[i][j] corresponds to the population density of the lattitude i-89.5 and longitude j-179.5
    public static float[][] pop_table;
    public static long totalPop = 0;
    public static long initialPop = 0;
    public static float[][] veg_table;
    public static float totalVeg = 0;

    public static bool gameOver = false;

    // All necessary CO2 variables
    private CO2Manager CO2Manage;
    private float CO2CurrentValue;
    private float CO2Ratio;
    private float CO2CriticalValue;

    // Rate at which population automatically increases over time
    private float popIncreaseRate;
    private float increasePop;

    // Rate at which UI is updated
    private float nextUpdateTime;
    private float updateRate;

    private bool haveTotalPopulation = false;

    // Start is called before the first frame update
    void Awake()
    {
        if (Application.platform != RuntimePlatform.Android)
        {
            using (var reader = new StreamReader(@"Assets/Data/population_data.csv"))
            {
                pop_table = new float[180][];

                var line = reader.ReadLine();
                int k = 0;
                while (!reader.EndOfStream)
                {
                    line = reader.ReadLine();
                    var values = line.Split(',');
                    pop_table[179 - k] = new float[360];
                    for (int i = 1; i < 361; i++)
                    {
                        if (values[i] == "99999.0")
                        {
                            pop_table[179 - k][i - 1] = 0;
                        }
                        else
                        {
                            pop_table[179 - k][i - 1] = float.Parse(values[i], NumberStyles.Float, new CultureInfo("en-US"));
                        }

                    }
                    k += 1;
                }
            }
            using (var reader = new StreamReader(@"Assets/Data/vegetation_data.csv"))
            {
                veg_table = new float[180][];

                var line = reader.ReadLine();
                int k = 0;
                while (!reader.EndOfStream)
                {
                    line = reader.ReadLine();
                    var values = line.Split(',');
                    veg_table[179 - k] = new float[360];
                    for (int i = 1; i < 361; i++)
                    {
                        if (values[i] == "99999.0")
                        {
                            veg_table[179 - k][i - 1] = 0;
                        }
                        else
                        {
                            veg_table[179 - k][i - 1] = float.Parse(values[i], NumberStyles.Float, new CultureInfo("en-US")) + 0.1f;
                        }
                    }
                    k += 1;
                }
            }
            totalVeg = TotalVegetation();
            totalPop = TotalPopulation();
            initialPop = totalPop;
            haveTotalPopulation = true;

            // Debug.Log("population before anything else is: " + totalPop);
        }
    }

    void Start() { 
        // Set population UI
        if (haveTotalPopulation)
        {
            popSlider.maxValue = initialPop;
            popSlider.value = totalPop;
        }

        popIncreaseRate = 1000;

        // Set CO2 variables
        CO2Manage = GetComponent<CO2Manager>();
        CO2CurrentValue = CO2Manage.currentCO2;
        CO2CriticalValue = CO2Manage.criticalCO2;

        // Set timer variables
        updateRate = 0.5f;
        nextUpdateTime = Time.time + updateRate;
    }

    // Update is called once per frame
    void Update()
    {
        if (Time.time > nextUpdateTime)
        {
            // Have population increase automatically over time, rate dependent on CO2 levels
            CO2CurrentValue = CO2Manage.currentCO2;
            CO2Ratio = ((CO2CriticalValue - CO2CurrentValue) / CO2CriticalValue) * 500000;
            if (CO2CurrentValue > 99)
            {
                CO2Ratio = CO2Ratio * 10;
            }

            increasePop = popIncreaseRate * CO2Ratio * Time.deltaTime;

            // Debug.Log("The CO2Ratio is: " + CO2Ratio);
            // Debug.Log("The increasePop is: " + increasePop);

            totalPop += Convert.ToInt64(increasePop);

            if (!haveTotalPopulation && initialPop > 0)
            {
                popSlider.maxValue = initialPop;
                haveTotalPopulation = true;
            }

            // NetworkDebugger.Log("TOTAL POPULATION: " + totalPop);
            // Debug.Log("TOTAL POPULATION: " + totalPop);

            // Update population UI
            popSlider.value = totalPop;

            if (totalPop < 100 && gameOver == false)
            {
                gameOver = true;
                gameOverScreen.SetActive(true);
            }
            
            // Set timer
            nextUpdateTime = Time.time + updateRate;
        }
    }

    public long TotalPopulation() {
        long pop = 0;
        for (int i = 0; i < 180; i++)
        {
            for (int j = 0; j < 360; j++)
            {
                pop += (int)(pop_table[i][j] * 12345 * Math.Cos(((double)i - 89.5) * Math.PI / 180d));
            }
        }
        return pop;
    }

    public long TotalVegetation()
    {
        long veg = 0;
        for (int i = 0; i < 180; i++)
        {
            for (int j = 0; j < 360; j++)
            {
                veg += (int)(veg_table[i][j] * 12345 * Math.Cos(((double)i - 89.5) * Math.PI / 180d));
            }
        }
        return veg;
    }

    public long Explosion(float latitude, float longitude, out long number_of_dead, out float dead_vegetation)
    {
        number_of_dead = 0;
        dead_vegetation = 0;
        for (int i = 0; i < 180; i++) {
            for (int j = 0; j < 360; j++) {
                var x = (latitude - (i - 89.5f)) * Math.Cos((longitude + ((float)j - 89.5f))*Math.PI/180 / 2);
                var y = (longitude - (j - 179.5f));
                var explosionDistance = Math.Sqrt(x * x + y * y) ;
                if (explosionDistance < exterminationRadius) {
                    float density_dead = pop_table[i][j];
                    pop_table[i][j] = 0;
                    number_of_dead += (long) (density_dead* 12345 * Math.Cos(((double)i - 89.5)*Math.PI/ 180d));
                    dead_vegetation += (float) (veg_table[i][j] * 12345 * Math.Cos(((double)i - 89.5) * Math.PI / 180d));
                    veg_table[i][j] = 0;
                }
                else if(explosionDistance < effectRadius){
                    float proportion = (effectRadius - (float) explosionDistance) / (effectRadius - exterminationRadius);
                    float density_dead = proportion*pop_table[i][j];
                    number_of_dead += (long)(density_dead * 12345 * Math.Cos(((double)i - 89.5) * Math.PI / 180d));
                    pop_table[i][j] *= (1 - proportion);
                    dead_vegetation += (float)(veg_table[i][j]*proportion* 12345 * Math.Cos(((double)i - 89.5) * Math.PI / 180d));
                    veg_table[i][j] *= (1 - proportion);
                }
            }
        }
        //Debug.Log("Hit " + latitude + ";" + longitude + ": " + number_of_dead + " dead.");
        totalPop -= (number_of_dead);
        totalVeg -= dead_vegetation;

        return totalPop;
    }
    public long Explosion(Vector2 v, out long number_of_dead, out float dead_vegetation) {
        return Explosion(v.x, v.y, out number_of_dead, out dead_vegetation);
    }
}
