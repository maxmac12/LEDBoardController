using System;
using System.Collections.ObjectModel;
using System.ComponentModel;

public class ConsoleContent : INotifyPropertyChanged
{
    string consoleInput = string.Empty;
    ObservableCollection<string> consoleOutput = new ObservableCollection<string>();

    public string ConsoleInput
    {
        get
        {
            return consoleInput;
        }
        set
        {
            consoleInput = value;
            OnPropertyChanged("ConsoleInput");
        }
    }

    public ObservableCollection<string> ConsoleOutput
    {
        get
        {
            return consoleOutput;
        }
        set
        {
            consoleOutput = value;
            OnPropertyChanged("ConsoleOutput");
        }
    }

    public void RunCommand()
    {
        ConsoleInput = String.Empty;
    }

    public event PropertyChangedEventHandler PropertyChanged;
    void OnPropertyChanged(string propertyName)
    {
        if (null != PropertyChanged)
            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
    }
}