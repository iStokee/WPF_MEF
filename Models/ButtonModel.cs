using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace WPF_MEF.Models
{
	public class ButtonModel
	{
		public string Name { get; set; }
		public ICommand Command { get; set; }
	}
}
