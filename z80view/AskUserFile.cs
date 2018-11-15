using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using z80emu;

namespace z80view
{
    public interface IAskUserFile
    {
        Task<string> AskFile();
    }

    public class AskUserFile : IAskUserFile
    {
        public async Task<string> AskFile()
        {
            var filter = new FileDialogFilter() 
            {
                Name = "Z80 image files",
                Extensions = new List<string> { "z80" }
            };

            var openDialog = new OpenFileDialog();
            openDialog.Title = "Select z80 file";
            openDialog.AllowMultiple = false;
            openDialog.Filters.Add(filter);

            var files = await openDialog.ShowAsync();
            if (files != null && files.Length != 0) 
            {
                return files[0];
            }

            return null;
        }
    }
}
