﻿using System;
using AudioSwitcher.AudioApi;
using AudioSwitcher.AudioApi.CoreAudio;

namespace WindowsSoundControl
{
    public class AudioDevice
    {
        private readonly CoreAudioDevice device;
        private readonly MuteObserver muteObserver;
        private DateTime lastAction;

        public AudioDevice(string pid)
        {
            foreach (CoreAudioDevice device in new CoreAudioController().GetDevices(DeviceState.Active))
                if (device.RealId.Equals(pid))
                {
                    this.device = device;
                    break;
                }

            this.muteObserver = new MuteObserver(this);
            this.muteObserver.Subscribe(this.device.MuteChanged);

            this.lastAction = DateTime.Now;
        }

        public void ToggleMute()
        {
            double timeelapsed = DateTime.Now.Subtract(this.lastAction).TotalMilliseconds;
            if (timeelapsed < 50)
            {
                Console.WriteLine("Too many actions for AudioDevice. {0}", timeelapsed);
                return;
            }
            this.lastAction = DateTime.Now;
            this.device.Mute(!this.device.IsMuted);
        }

        public bool IsMuted()
        {
            return this.device.IsMuted;
        }

        public void SetVolume(double volume)
        {
            double timeelapsed = DateTime.Now.Subtract(this.lastAction).TotalMilliseconds;
            if (timeelapsed < 10)
            {
                Console.WriteLine("Too many actions for AudioDevice. {0}", timeelapsed);
                return;
            }
            this.lastAction = DateTime.Now;
            this.device.Volume = volume;
        }

        public void Dispose()
        {
            this.muteObserver.Unsubscribe();
        }

        public event MuteStateChangedEventHandler OnMuteStateChanged;
        public class OnMuteStateChangedEventArgs : EventArgs
        {
            public bool muted;
        }
        public delegate void MuteStateChangedEventHandler(object sender, OnMuteStateChangedEventArgs e);

        internal class MuteObserver : IObserver<DeviceMuteChangedArgs>
        {
            private IDisposable subscribed;
            private readonly AudioDevice parent;

            public MuteObserver(AudioDevice parent)
            {
                this.parent = parent;
            }

            public virtual void Subscribe(IObservable<DeviceMuteChangedArgs> provider)
            {
                this.subscribed = provider.Subscribe(this);
            }

            public virtual void Unsubscribe()
            {
                this.subscribed.Dispose();
            }

            public void OnCompleted()
            {
                this.Unsubscribe();
                throw new NotImplementedException();
            }

            public void OnError(Exception error)
            {
                throw error;
            }

            public void OnNext(DeviceMuteChangedArgs value)
            {
                this.parent.OnMuteStateChanged?.Invoke(this.parent, new OnMuteStateChangedEventArgs()
                {
                    muted = value.IsMuted
                });
            }
        }
    }
}