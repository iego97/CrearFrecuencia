using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System.Windows.Threading;
using System.IO;

namespace Playback
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private Mp3FileReader reader;
        private WaveOutEvent output;
        DispatcherTimer timer;
        bool dragging = false;
        private VolumeWaveProvider16 volumeProvider;
        SignalGenerator signalGenerator;

        public MainWindow()
        {
            InitializeComponent();
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(500);
            timer.Tick += OnTimerTick;

            LlenarComboDispositivos();
        }

        private void LlenarComboDispositivos()
        {
            for (int i=0; i<WaveOut.DeviceCount; i++)
            {
                WaveOutCapabilities capacidades =
                    WaveOut.GetCapabilities(i);
                cbDispositivos.Items.Add(capacidades.ProductName);
            }
            cbDispositivos.SelectedIndex = 0;
        }

        private void OnTimerTick(object sender, EventArgs e)
        {
            if (reader != null)
            {
                string tiempoActual = reader.CurrentTime.ToString();
                tiempoActual = tiempoActual.Substring(0, 8);
                lblPosition.Text = tiempoActual;
                if (!dragging)
                {
                    sldPosition.Value = reader.CurrentTime.TotalSeconds;
                }
            } 
        }


        private void btnBuscar_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();

            if (openFileDialog.ShowDialog() == true)
            {
                txtRuta.Text = openFileDialog.FileName;

            }
        }

        private void btnPlay_Click(object sender, RoutedEventArgs e)
        {
            if(output != null && 
                output.PlaybackState == PlaybackState.Paused)
            {
                output.Play();
                btnPlay.IsEnabled = false;
                btnPause.IsEnabled = true;
                btnStop.IsEnabled = true;
            } else
            {
                if (txtRuta.Text != null && txtRuta.Text != "")
                {
                    output = new WaveOutEvent();
                    output.PlaybackStopped += OnPlaybackStop;
                    reader = new Mp3FileReader(txtRuta.Text);

                    //Configuraciones de WaveOut
                    output.DeviceNumber = cbDispositivos.SelectedIndex;
                    output.NumberOfBuffers = 2;
                    output.DesiredLatency = 150;


                    volumeProvider =
                        new VolumeWaveProvider16(reader);
                    volumeProvider.Volume =
                        (float)sldVolumen.Value;


                    output.Init(volumeProvider);
                    output.Play();

                    btnStop.IsEnabled = true;
                    btnPause.IsEnabled = true;
                    btnPlay.IsEnabled = false;

                    //00:00:15.465456456
                    lblDuration.Text =
                        reader.TotalTime.ToString().Substring(0, 8);
                    lblPosition.Text =
                        reader.CurrentTime.ToString().Substring(0, 8);

                    sldPosition.Maximum = reader.TotalTime.TotalSeconds;
                    sldPosition.Value = 0;

                    timer.Start();



                }
                else
                {
                    //Avisarle al usuario que elija un archivo
                }
            }
            
        }

        private void OnPlaybackStop(object sender, StoppedEventArgs e)
        {
            reader.Dispose();
            output.Dispose();
            timer.Stop();
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            if (output != null)
            {
                output.Stop();
                btnPlay.IsEnabled = true;
                btnPause.IsEnabled = false;
                btnStop.IsEnabled = false;
            }
        }

       

        private void sldPosition_dragCompleted(object sender, RoutedEventArgs e)
        {
            if (reader != null && output != null &&
                (output.PlaybackState == PlaybackState.Playing || 
                output.PlaybackState == PlaybackState.Paused))
            {
                reader.CurrentTime = TimeSpan.FromSeconds(sldPosition.Value);
                dragging = false;
            } 
        }

        private void sldPostion_dragStarted(object sender, RoutedEventArgs e)
        {
            if (reader != null)
            {
                dragging = true;
            }
        }

        private void btnPause_Click(object sender, RoutedEventArgs e)
        {
            if (output != null)
            {
                if (output.PlaybackState == PlaybackState.Playing)
                {
                    output.Pause();
                    btnPause.IsEnabled = false;
                    btnStop.IsEnabled = false;
                    btnPlay.IsEnabled = true;
                }
            }
        }

        private void sldVolumen_DragCompleted(object sender, RoutedEventArgs e)
        {

        }

        private void sldVolumen_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (volumeProvider != null)
            {
                volumeProvider.Volume =
                    (float)sldVolumen.Value;
            }
        }

        private void btncortar_Click(object sender, RoutedEventArgs e)
        {
             // Verificar que haya ruta
             if (txtRuta.Text != null && txtRuta.Text != string.Empty)
            {
                var reader = new Mp3FileReader(txtRuta.Text);
                var writer = File.Create("cortado.mp3");
                var posicionInicial = TimeSpan.FromSeconds(10);
                var posicionFinal = TimeSpan.FromSeconds(20);

                reader.CurrentTime = posicionInicial;
                while (reader.CurrentTime < posicionFinal)
                {
                    var frame = reader.ReadNextFrame();

                    if (frame == null)
                    {
                        break;
                    }
                    writer.Write(frame.RawData,0,frame.RawData.Length);

                }
                writer.Dispose();
                   
             
            }
        }

        //Va a generar una señal con una frecuencia de 440
        //y la guardará en un wav

        private void btnCrearFrecuencia_Click(object sender, RoutedEventArgs e)
        {

            

            var sampleRate = 44100;
            var channelCount = 1;
            var signalGenerator = new SignalGenerator(sampleRate, channelCount);
            signalGenerator.Type = SignalGeneratorType.Sin;
            signalGenerator.Frequency = Int32.Parse(txtFrecuencia.Text);
            signalGenerator.Gain = 0.5;

            var waveFormat = new WaveFormat(sampleRate, 16, channelCount);
            var writer = new WaveFileWriter(txtArchivo.Text, waveFormat);
            var muestrasPorSegundo = sampleRate * channelCount;
            var buffer = new float[muestrasPorSegundo];

            for (int i = 0; i < Int32.Parse(txtSegundos.Text); i++)
            {
                var muestras = signalGenerator.Read(buffer,0,muestrasPorSegundo);
                writer.WriteSamples(buffer, 0, muestras);
            }
            writer.Dispose();
        }

        private void btnOffset_Click(object sender, RoutedEventArgs e)
        {
            var sampleRate = 44100;
            var channelCount = 1;
            var seconds = 10;
            var signalGenerator = new SignalGenerator();
            signalGenerator.Frequency = 750;
            signalGenerator.Gain = 0.5;
            var offsetProvider =
                  new OffsetSampleProvider(signalGenerator);
            offsetProvider.TakeSamples = sampleRate * seconds * channelCount;

            WaveFileWriter.CreateWaveFile16("sonidito.wav", offsetProvider);
        }

        private void btnReproducirSeñal_Click(object sender, RoutedEventArgs e)
        {
            var sampleRate = 44100;
            var channelCount = 1;
            var seconds = 10;
            signalGenerator = new SignalGenerator();
            signalGenerator.Frequency = 750;
            signalGenerator.Gain = 0.5;
            var offsetProvider =
                  new OffsetSampleProvider(signalGenerator);
            offsetProvider.TakeSamples = sampleRate * seconds * channelCount;

            output = new WaveOutEvent();
            output.Init(signalGenerator);
            output.Play();

        }

        private void sldFrecuencia_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if(sldFrecuencia != null && signalGenerator != null)
            {
                signalGenerator.Frequency = sldFrecuencia.Value;
            }
               
        }
    }
}