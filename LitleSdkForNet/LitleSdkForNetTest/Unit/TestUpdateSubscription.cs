using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using Litle.Sdk;
using Moq;
using System.Text.RegularExpressions;


namespace Litle.Sdk.Test.Unit
{
    [TestFixture]
    class TestUpdateSubscription
    {        
        private LitleOnline litle;

        [OneTimeSetUp]
        public void SetUpLitle()
        {
            litle = new LitleOnline();
        }

        [Test]
        public void TestSimple()
        {
            updateSubscription update = new updateSubscription();
            update.billingDate = new DateTime(2002, 10, 9);
            contact billToAddress = new contact();
            billToAddress.name = "Greg Dake";
            billToAddress.city = "Lowell";
            billToAddress.state = "MA";
            billToAddress.email = "sdksupport@litle.com";
            update.billToAddress = billToAddress;
            cardType card = new cardType();
            card.number = "4100000000000001";
            card.expDate = "1215";
            card.type = methodOfPaymentTypeEnum.VI;
            update.card = card;
            update.planCode = "abcdefg";
            update.subscriptionId = 12345;
           
            var mock = new Mock<Communications>();

            mock.Setup(Communications => Communications.HttpPost(It.IsRegex(".*<litleOnlineRequest.*?<updateSubscription>\n<subscriptionId>12345</subscriptionId>\n<planCode>abcdefg</planCode>\n<billToAddress>\n<name>Greg Dake</name>.*?</billToAddress>\n<card>\n<type>VI</type>.*?</card>\n<billingDate>2002-10-09</billingDate>\n</updateSubscription>\n</litleOnlineRequest>.*?.*", RegexOptions.Singleline), It.IsAny<Dictionary<String, String>>()))
                .Returns("<litleOnlineResponse version='8.20' response='0' message='Valid Format' xmlns='http://www.litle.com/schema'><updateSubscriptionResponse ><litleTxnId>456</litleTxnId><response>000</response><message>Approved</message><responseTime>2013-09-04</responseTime><subscriptionId>12345</subscriptionId></updateSubscriptionResponse></litleOnlineResponse>");
     
            Communications mockedCommunication = mock.Object;
            litle.setCommunication(mockedCommunication);
            updateSubscriptionResponse response = litle.UpdateSubscription(update);
            Assert.AreEqual("12345", response.subscriptionId);
            Assert.AreEqual("456", response.litleTxnId);
            Assert.AreEqual("000", response.response);
            Assert.NotNull(response.responseTime);
        }


    }
}
