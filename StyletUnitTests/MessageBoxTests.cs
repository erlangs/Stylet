﻿using Moq;
using NUnit.Framework;
using Stylet;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace StyletUnitTests
{
    [TestFixture]
    public class MessageBoxTests
    {
        private class MyMessageBoxViewModel : MessageBoxViewModel
        {
            public new void OnViewLoaded()
            {
                base.OnViewLoaded();
            }
        }

        private MessageBoxViewModel vm;

        [SetUp]
        public void SetUp()
        {
            this.vm = new MessageBoxViewModel();
        }

        [Test]
        public void SetsTextCorrectly()
        {
            this.vm.Setup("this is the text", null);
            Assert.AreEqual("this is the text", this.vm.Text);
        }

        [Test]
        public void SetsTitleCorrectly()
        {
            this.vm.Setup(null, "this is the title");
            Assert.AreEqual("this is the title", this.vm.DisplayName);
        }

        [Test]
        public void DisplaysRequestedButtons()
        {
            this.vm.Setup(null, null, new MessageBoxConfig() { Buttons = MessageBoxButton.OKCancel });
            var buttons = vm.ButtonList.ToList();
            Assert.AreEqual(2, buttons.Count);
            Assert.AreEqual("OK", buttons[0].Label);
            Assert.AreEqual(MessageBoxResult.OK, buttons[0].Value);
            Assert.AreEqual("Cancel", buttons[1].Label);
            Assert.AreEqual(MessageBoxResult.Cancel, buttons[1].Value);
        }

        [Test]
        public void SetsDefaultButtonToTheRequestedButton()
        {
            this.vm.Setup(null, null, new MessageBoxConfig() { Buttons = MessageBoxButton.OKCancel, DefaultButton = MessageBoxResult.Cancel });
            Assert.AreEqual(this.vm.ButtonList.ElementAt(1), this.vm.DefaultButton);
        }

        [Test]
        public void SetsDefaultToLeftmostButtonIfDefaultRequested()
        {
            this.vm.Setup(null, null, new MessageBoxConfig() { Buttons = MessageBoxButton.YesNoCancel });
            Assert.AreEqual(this.vm.ButtonList.ElementAt(0), this.vm.DefaultButton);
        }

        [Test]
        public void ThrowsIfTheRequestedDefaultButtonIsNotDisplayed()
        {
            Assert.Throws<ArgumentException>(() => this.vm.Setup(null, null, new MessageBoxConfig() { Buttons = MessageBoxButton.OKCancel, DefaultButton = MessageBoxResult.Yes }));
        }

        [Test]
        public void SetsCancelButtonToTheRequestedButton()
        {
            this.vm.Setup(null, null, new MessageBoxConfig() { Buttons = MessageBoxButton.YesNoCancel, CancelButton = MessageBoxResult.No });
            Assert.AreEqual(this.vm.ButtonList.ElementAt(1), this.vm.CancelButton);
        }

        [Test]
        public void SetsCancelToRighmostButtonIfDefaultRequested()
        {
            this.vm.Setup(null, null, new MessageBoxConfig() { Buttons = MessageBoxButton.OKCancel });
            Assert.AreEqual(this.vm.ButtonList.ElementAt(1), this.vm.CancelButton);
        }

        [Test]
        public void ThrowsIfRequestedCancelButtonIsNotDisplayed()
        {
            Assert.Throws<ArgumentException>(() => this.vm.Setup(null, null, new MessageBoxConfig() { CancelButton = MessageBoxResult.No }));
        }

        [Test]
        public void SetsIconCorrectly()
        {
            this.vm.Setup(null, null, new MessageBoxConfig() { Icon = MessageBoxImage.Exclamation });
            Assert.AreEqual(SystemIcons.Exclamation, this.vm.ImageIcon);
        }

        [Test]
        public void SetsResultAndClosesWhenButtonClicked()
        {
            var parent = new Mock<IChildDelegate>();
            this.vm.Parent = parent.Object;
            this.vm.Setup(null, null);
            
            this.vm.ButtonClicked(MessageBoxResult.No);

            parent.Verify(x => x.CloseItem(this.vm, true));
            Assert.AreEqual(MessageBoxResult.No, this.vm.ClickedButton);
        }

        [Test]
        public void PlaysNoSoundIfNoSoundToPlay()
        {
            var vm = new MyMessageBoxViewModel();
            // Can't test it actually playing the sound
            vm.Setup(null, null);
            Assert.DoesNotThrow(() => vm.OnViewLoaded());
        }

        [Test]
        public void ButtonTextOverridesWork()
        {
            this.vm.Setup(null, null, new MessageBoxConfig()
            {
                Buttons = MessageBoxButton.OKCancel,
                ButtonLabels = new Dictionary<MessageBoxResult, string>()
                {
                    { MessageBoxResult.Cancel, "YAY!" },
                }
            });
            Assert.AreEqual("OK", this.vm.ButtonList.ElementAt(0).Label);
            Assert.AreEqual("YAY!", this.vm.ButtonList.ElementAt(1).Label);
        }

        [Test]
        public void FlowsLeftToRightByDefault()
        {
            this.vm.Setup(null, null);
            Assert.AreEqual(FlowDirection.LeftToRight, this.vm.FlowDirection);
        }

        [Test]
        public void FlowsRightToLeftIfRequested()
        {
            this.vm.Setup(null, null, new MessageBoxConfig() { Options = MessageBoxOptions.RtlReading });
            Assert.AreEqual(FlowDirection.RightToLeft, this.vm.FlowDirection);
        }

        [Test]
        public void AlignsLeftIfLeftToRightByDefault()
        {
            this.vm.Setup(null, null);
            Assert.AreEqual(TextAlignment.Left, this.vm.TextAlignment);
        }

        [Test]
        public void AlignsRightIfLeftToRightAndRequested()
        {
            this.vm.Setup(null, null, new MessageBoxConfig() { Options = MessageBoxOptions.RightAlign });
            Assert.AreEqual(TextAlignment.Right, this.vm.TextAlignment);
        }

        [Test]
        public void AlignsRightIfRightToLeftByDefault()
        {
            this.vm.Setup(null, null, new MessageBoxConfig() { Options = MessageBoxOptions.RtlReading });
            Assert.AreEqual(TextAlignment.Right, this.vm.TextAlignment);
        }

        [Test]
        public void AlignsLeftIfRightToLeftAndRequested()
        {
            this.vm.Setup(null, null, new MessageBoxConfig() { Options = MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading });
            Assert.AreEqual(TextAlignment.Left, this.vm.TextAlignment);
        }
    }
}