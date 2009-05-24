﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Renamer.Logging
{
    /// <summary>
    /// Logger which manages all the loggers registered in System
    /// </summary>
    public class Logger : ILogger
    {

        private static Logger instance = null;
        private static object m_lock = new object();


        protected delegate void LoggerDelegate(string Message, LogLevel level);

        protected LoggerDelegate allLogers;

        public static Logger Instance {
            get {
                if (instance == null) {
                    lock (m_lock) { if (instance == null) instance = new Logger(); }
                }
                return instance;
            }
        }

        // pass a ILoggers that are part of this composite logger
        protected Logger() {
        }

        public void addLogger(ILogger logger) {
            this.allLogers += logger.LogMessage;
        }
        public void addLogger(params ILogger[] logger) {
            foreach (ILogger log in logger) {
                this.allLogers += log.LogMessage;
            }
        }

        public void removeLogger(ILogger logger) {
            this.allLogers -= logger.LogMessage;
        }
        public void removeLogger(params ILogger[] logger) {
            foreach (ILogger log in logger) {
                this.allLogers -= log.LogMessage;
            }
        }

        public void LogMessage(string strMessage, LogLevel level) {
            if (this.allLogers != null) {
                this.allLogers(strMessage, level);
            }
        }
    }
}
