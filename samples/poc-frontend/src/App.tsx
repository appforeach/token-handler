import React, { useState } from 'react';
import './App.css';

function App() {
  const [sessionId, setSessionId] = useState<string | null>(null);
  const [loginStatus, setLoginStatus] = useState<string>('');
  const [weather, setWeather] = useState<any>(null);
  const [internalData, setInternalApData] = useState<any>(null);

  
  const handleLogin = async () => {
    setLoginStatus('');

    window.location.href = 'http://localhost:5198/TokenHandler/authorize';
  };

    const handleWeather = async () => {
      // Check if sessionId exists in state and cookies
      const getCookie = (name: string) => {
        const match = document.cookie.match(new RegExp('(^| )' + name + '=([^;]+)'));
        return match ? match[2] : null;
      };
      // Ensure fetch sends cookies (including HttpOnly session-id) by setting credentials: 'include'
      // const cookieSessionId = getCookie('session-id');

      // debugger;

      // if (!sessionId && !cookieSessionId) {
      //   setWeather('No session. Please login first.');
      //   return;
      // }
      // const token = sessionId || cookieSessionId;
      // console.log('Using token:', token);


    // Call weather API with Bearer token
    const weatherResp = await fetch('http://localhost:5198/weatherforecast', {
      // headers: { Authorization: `Bearer ${token}` },
      credentials: 'include', // This ensures browser cookies are sent
    });
    if (!weatherResp.ok) {
      setWeather('Weather API call failed.');
      return;
    }
    const weatherData = await weatherResp.json();
    setWeather(weatherData);
  };

      const handleInternalApi = async () => {
      // Check if sessionId exists in state and cookies
      const getCookie = (name: string) => {
        const match = document.cookie.match(new RegExp('(^| )' + name + '=([^;]+)'));
        return match ? match[2] : null;
      };


    // Call weather API with Bearer token
    const internalDataResp = await fetch('http://localhost:5198/InternalDataProxy', {
      // headers: { Authorization: `Bearer ${token}` },
      credentials: 'include', // This ensures browser cookies are sent
    });
    if (!internalDataResp.ok) {
      setInternalApData('InternalDataProxy API call failed.');
      return;
    }
    const internalData = await internalDataResp.json();
    setInternalApData(internalData);
  };

  return (
    <div style={{ padding: 40 }}>
      <h2>POC Simple Frontend</h2>
      <button onClick={handleLogin}>Login</button>
      <span style={{ marginLeft: 10 }}>{loginStatus}</span>
      <br /><br />
      <button onClick={handleWeather}>BFF &#8594; API</button>
      <div style={{ marginTop: 20 }}>
        {weather && (
          <pre>{typeof weather === 'string' ? weather : JSON.stringify(weather, null, 2)}</pre>
        )}
      </div>

      <button onClick={handleInternalApi}>BFF &#8594; API &#8594; Internal API</button>
      <div style={{ marginTop: 20 }}>
        {internalData && (
          <pre>{typeof internalData === 'string' ? internalData : JSON.stringify(internalData, null, 2)}</pre>
        )}
      </div>
    </div>
  );
}

export default App;
