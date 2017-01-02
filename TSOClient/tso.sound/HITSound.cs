﻿/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FSO.HIT
{
    public abstract class HITSound
    {
        protected bool VolumeSet;
        protected float Volume = 1;
        protected float Pan;

        protected bool EverHadOwners; //if we never had owners, don't kill the thread. (ui sounds)
        public int LastMainOwner = -1;
        protected List<int> Owners;

        public bool Dead;

        public HITSound()
        {
            Owners = new List<int>();
        }

        public abstract bool Tick();

        public bool SetVolume(float volume, float pan, int ownerID)
        {
            bool ownerChange = false;
            if (VolumeSet)
            {
                if (volume > Volume)
                {
                    if (LastMainOwner != ownerID) { LastMainOwner = ownerID; ownerChange = true; }
                    Volume = volume;
                    Pan = pan;
                    return true;
                }
                return false;
            }
            else
            {
                VolumeSet = true;
                if (LastMainOwner != ownerID) { LastMainOwner = ownerID; ownerChange = true; }
                Volume = volume;
                Pan = pan;
                return true;
            }
        }

        public void AddOwner(int id)
        {
            EverHadOwners = true;
            Owners.Add(id);
        }

        public void RemoveOwner(int id)
        {
            Owners.Remove(id);
        }

        public bool AlreadyOwns(int id)
        {
            return Owners.Contains(id);
        }
    }
}
