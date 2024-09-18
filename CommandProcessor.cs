using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QQBot
{
    public interface CommandProcessor
    {
        string Process(SparkSession session, string[] args);
    }

    public class Help : CommandProcessor
    {
        public string Process(SparkSession session, string[] args)
        {
            string answer = "可用指令如下:\n" +
                "/help - 查看可用指令\n" +
                "/ask <问题>- 向大模型提问（若没有会话，则会自动创建）\n" +
                "/new <设定Prompt>- 创建新的会话，并设置Prompt\n" +
                "/history - 查看会话历史记录";
            return answer;
        }
    }
    public class Ask : CommandProcessor
    {
        public string Process(SparkSession session, string[] args)
        {
            string answer = "";
            if (args.Length >= 2)
            {
                string question = args[1];
                Console.WriteLine("Asking: " + question);
                answer = session.Ask(question).Result;
            }
            return answer;
        }
    }

    public class History : CommandProcessor
    {
        public string Process(SparkSession session, string[] args)
        {
            string answer = "";
            if (session != null)
            {
                answer = session.GetHistory();
            }
            else
            {
                answer = "当前没有会话";
            }
            return answer;         
        }
    }

}

