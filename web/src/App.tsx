import { useRoller } from './state/useRoller';
import { RollerPanel } from './components/RollerPanel';
import './App.css';

function App() {
  const roller = useRoller();

  return (
    <main className="app">
      <h1 className="app-title">Dice Roller</h1>
      <RollerPanel roller={roller} />
    </main>
  );
}

export default App;
