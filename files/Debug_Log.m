% Read the text file and use jsondecode to produce a cell array of
% structures.

events = jsondecode(fileread("Debug_Log.json"));

% The structures will have different fields, so we must use loops to filter
% them before they can be combined into a single struct array or table.

% Below, find all the events of type SpawnObject, and combine them into a
% new array.

spawn = [];

for i = 1:numel(events)
   % The curly braces access the contents of the cell i, which is the
   % struct itself.
   s = events{i};
   if categorical(cellstr(s.event)) == categorical("SpawnObject")
       spawn = [spawn; s];
   end
end

% Convert the new array into a table
T = struct2table(spawn);

% Use the table to change the type of the sceneid column so we can easily
% split the events by which peer they are from.
T.peer = categorical(T.peer);

% Filter the events to keep only those emitted by the Peer that initiated
% the spawn
T = T(T.arg3,:);

% Plot the number of objects spawned over time, by each Peer
figure;
hold all;
peers = unique(T.peer);
for p = peers'
   spawned = T(T.peer == p,:);
   plot(spawned.ticks,1:size(spawned,1));
end

xlabel("Time (ticks)");
ylabel("Number of Objects");
legend(peers);