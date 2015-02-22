#region copyright
// Copyright (c) 2015 Wm. Barrett Simms wbsimms.com
//
// Permission is hereby granted, free of charge, to any person 
// obtaining a copy of this software and associated documentation 
// files (the "Software"), to deal in the Software without restriction, including 
// without limitation the rights to use, copy, modify, merge, publish, 
// distribute, sublicense, and/or sell copies of the Software, 
// and to permit persons to whom the Software is furnished to do so, 
// subject to the following conditions:
//
// The above copyright notice and this permission notice shall be 
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, 
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A 
// PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR 
// COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER 
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, 
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
#endregion
using System;
using System.Collections;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Presentation;
using Microsoft.SPOT.Presentation.Controls;
using Microsoft.SPOT.Presentation.Media;
using Microsoft.SPOT.Presentation.Shapes;
using Microsoft.SPOT.Touch;

using Gadgeteer.Networking;
using GT = Gadgeteer;
using GTM = Gadgeteer.Modules;
using Gadgeteer.SocketInterfaces;
using Microsoft.SPOT.Hardware;
using Button = Gadgeteer.Modules.GHIElectronics.Button;
using Gadgeteer.Modules.GHIElectronics;

namespace IOSerialFun
{
    public partial class Program
    {
        private DigitalInput input; // Q7
        private DigitalOutput output_pl, output_cp, output_ce;
        GT.Timer dataReadTimer = new GT.Timer(100);
        private DigitalOutput output_ser, output_clk, output_srclk;


        void ProgramStarted()
        {
            // My breakout board is on socket 1
            // 74HC165
            output_ser = breadBoardX1.CreateDigitalOutput(GT.Socket.Pin.Six, false);  // data
            output_clk = breadBoardX1.CreateDigitalOutput(GT.Socket.Pin.Seven, false);  // clock
            output_srclk = breadBoardX1.CreateDigitalOutput(GT.Socket.Pin.Eight, false); // latch

            // 74HC165
            input = breadBoardX1.CreateDigitalInput(GT.Socket.Pin.Nine, GlitchFilterMode.Off, ResistorMode.Disabled); // data
            output_cp = breadBoardX1.CreateDigitalOutput(GT.Socket.Pin.Four, false);  // clock input
            output_ce = breadBoardX1.CreateDigitalOutput(GT.Socket.Pin.Five, false);  // clock enable
            output_pl = breadBoardX1.CreateDigitalOutput(GT.Socket.Pin.Three, false); // load
            Debug.Print("Program Started");
            button.ButtonPressed += button_ButtonPressed;
            Debug.Print("Setting up 74HC165");
            output_pl.Write(true);
            output_ce.Write(true);
            output_cp.Write(false);
            dataReadTimer.Tick += dataReadTimer_Tick;
        }

        void dataReadTimer_Tick(GT.Timer timer)
        {
            output_pl.Write(false);
            output_pl.Write(true);
            output_ce.Write(false);

            int retval = 0;
            for (int i = 0; i <= 7; i++)
            {
                retval = retval << 1;
                if (input.Read())
                {
                    retval = retval + 1;
                }
                Clock();
            }
            output_ce.Write(true);
            Debug.Print(retval.ToString());
//            if (retval > 0) // uncomment if you want to keep the lights on
                DisplayLeds(retval);
        }

        private void DisplayLeds(int retval)
        {
            output_srclk.Write(false);
            // Bit reversal :)
            int copy = (int)((((ulong)retval * 0x0202020202UL) & 0x010884422010UL) % 1023);
            for (int i = 0; i <= 7; i++)
            {
                int x = copy & 1;
                if (x == 0)
                {
                    output_ser.Write(false);
                }
                else
                {
                    output_ser.Write(true);
                }
                output_clk.Write(true);
                output_clk.Write(false);
                copy = copy >> 1;
            }
            output_srclk.Write(true);
            output_srclk.Write(false);

        }

        private bool timerStarted = false;
        void button_ButtonPressed(Button sender, Button.ButtonState state)
        {
            if (!timerStarted)
            {
                dataReadTimer.Start();
                timerStarted = true;
            }
            else
            {
                dataReadTimer.Stop();
                timerStarted = false;
            }
        }

        public void Clock()
        {
            output_cp.Write(false);
            output_cp.Write(true);
        }
    }
}
