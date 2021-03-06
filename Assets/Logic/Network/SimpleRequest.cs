﻿using System;
using Logic.Utilities;
using UnityEngine;
using UnityEngine.Networking;

namespace Logic.Network
{
    public class SimpleRequest
    {
        public static void Get(string url, Action<UnityWebRequest> success, Action<UnityWebRequest> serverFailure, Action<UnityWebRequest> networkFailure)
        {
            var www = UnityWebRequest.Get(url);
            www.SendWebRequest();

            while (!www.isDone) ;

            if (www.isNetworkError) networkFailure(www);
            else if (www.isHttpError) serverFailure(www);
            else success(www);
        }
        
        public static void Post(string url, WWWForm data, Action<UnityWebRequest> success, Action<UnityWebRequest> serverFailure, Action<UnityWebRequest> networkFailure)
        {
            var www = UnityWebRequest.Post(url, data);
            www.SendWebRequest();

            while (!www.isDone) ;

            if (www.isNetworkError) networkFailure(www);
            else if (www.isHttpError) serverFailure(www);
            else success(www);
        }
        
        public static void Get(string url, string username, string password, Action<UnityWebRequest> success, Action<UnityWebRequest> serverFailure, Action<UnityWebRequest> networkFailure)
        {
            var www = UnityWebRequest.Get(url);
            www.BasicAuth(username, password);
            www.SendWebRequest();

            while (!www.isDone) ;

            if (www.isNetworkError) networkFailure(www);
            else if (www.isHttpError) serverFailure(www);
            else success(www);
        }
        
        public static void Post(string url, string username, string password, WWWForm data, Action<UnityWebRequest> success, Action<UnityWebRequest> serverFailure, Action<UnityWebRequest> networkFailure)
        {
            var www = UnityWebRequest.Post(url, data);
            www.BasicAuth(username, password);
            www.SendWebRequest();

            while (!www.isDone) ;

            if (www.isNetworkError) networkFailure(www);
            else if (www.isHttpError) serverFailure(www);
            else success(www);
        }
    }
}